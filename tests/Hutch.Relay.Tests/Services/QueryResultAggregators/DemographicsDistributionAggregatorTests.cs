using System.Text.Json;
using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config;
using Hutch.Relay.Constants;
using Hutch.Relay.Models;
using Hutch.Relay.Services;
using Hutch.Relay.Services.JobResultAggregators;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services.QueryResultAggregators;

public class DemographicsDistributionAggregatorTests
{
  // We don't do in depth obfuscation tests here; they are done in the Obfuscator test suite.
  // But we do attempt to ensure that the Obfuscator is being applied to aggregate outputs
  // Note that obfuscation defaults will affect test cases, unless you manually configure them off!

  // Later testing
  //   - Test that the validated accumulator correctly finalises into expected outputs for a given input
  //      - Test `Process()` end-to-end much like CodeDistribution does today

  #region Test Data

  private static RelaySubTaskModel GenerateSubTaskWithResultsData(List<GenericDistributionRecord> data)
  {
    var subTaskId = Guid.NewGuid();
    var subNodeId = data.FirstOrDefault()?.Collection ?? Guid.NewGuid().ToString();

    return new()
    {
      Id = subTaskId,
      Owner = new() { Id = Guid.NewGuid(), Owner = "test_user" },
      RelayTask = new()
      {
        Id = Guid.NewGuid().ToString(),
        Collection = "parent_collection",
        Type = TaskTypes.TaskApi_DemographicsDistribution,
      },
      Result = JsonSerializer.Serialize(new JobResult
      {
        Uuid = subTaskId.ToString(),
        CollectionId = subNodeId,
        Results = new()
        {
          Count = data.Count,
          DatasetCount = 1,
          Files =
          [
            new ResultFile()
              .WithAnalysisFileName(AnalysisType.Distribution, DistributionCode.Demographics)
              .WithData(data)
          ]
        }
      })
    };
  }

  private readonly List<RelaySubTaskModel> ProcessSubTasks =
  [
    GenerateSubTaskWithResultsData([
      new()
      {
        Collection = "sub_collection1",
        Code = Demographics.Sex,
        Description = "Sex",
        Count = 145,
        Alternatives = "^MALE|70^FEMALE|75^",
        Dataset = "person",
        Category = "Demographics"
      },
      new()
      {
        Collection = "sub_collection1",
        Code = Demographics.Genomics,
        Description = "Genomics",
        Count = 1005,
        Alternatives = "^No|1005^Imputed whole genome data|0^",
        Dataset = "person",
        Category = "Demographics"
      },
    ]),
    GenerateSubTaskWithResultsData([
      new()
      {
        Collection = "sub_collection2",
        Code = Demographics.Sex,
        Description = "Sex",
        Count = 1435,
        Alternatives = "^MALE|720^FEMALE|715^",
        Dataset = "person",
        Category = "Demographics"
      },
      new()
      {
        Collection = "sub_collection2",
        Code = Demographics.Genomics,
        Description = "Genomics",
        Count = 1477,
        Alternatives = "^No|1321^Imputed whole genome data|156^",
        Dataset = "person",
        Category = "Demographics"
      },
    ])
  ];

  private readonly DemographicsAccumulator _singleCodeAccumulator =
    new("test_collection")
    {
      Alternatives =
      {
        ["EXISTING_CODE"] = new(new()
        {
          Collection = "sub_collection",
          Code = "EXISTING_CODE",
          Description = "TEST_DESCRIPTION",
          Alternatives = "^BRACKET1|50^BRACKET2|70^",
          Dataset = "Person",
          Category = "Demographic"
        })
        {
          Alternatives =
          {
            ["BRACKET1"] = [50],
            ["BRACKET2"] = [70]
          }
        }
      }
    };

  private readonly DemographicsAccumulator _multiCodeAccumulator =
    new("test_collection")
    {
      Alternatives =
      {
        ["CODE1"] = new(new()
        {
          Collection = "sub_collection",
          Code = "CODE1",
          Description = "TEST_DESCRIPTION",
          Alternatives = "^BRACKET1|50^BRACKET2|70^"
        })
        {
          Alternatives =
          {
            ["BRACKET1"] = [50],
            ["BRACKET2"] = [70, 80]
          }
        },
        ["CODE2"] = new(new()
        {
          Collection = "sub_collection2",
          Code = "CODE2",
          Description = "TEST_DESCRIPTION",
          Alternatives = "^BRACKET_A|20^BRACKET2_B|320^"
        })
        {
          Alternatives =
          {
            ["BRACKET_A"] = [20, 54, 7], // 81
            ["BRACKET_B"] = [320, 80, 67, 90], // 557
            ["BRACKET_C"] = [32] // 32
          }
        }
      }
    };

  #endregion

  #region AccumulateData

  [Fact]
  public void AccumulateData_WhenFirstRowForCode_CorrectAlternativesAccumulation()
  {
    // When a code isn't already in the accumulated state:
    //   - base record is set to the record in question
    //   - alternatives are initialised with values from the record in question

    var initialAccumulator = _singleCodeAccumulator;

    List<DemographicsDistributionRecord> inputRecords =
    [
      new() { Code = Demographics.Sex, Collection = "sub_collection2", Alternatives = "^MALE|155^FEMALE|135^" }
    ];

    // Much of our expected state has to be redefined manually due to mutable reference types
    var expectedSexBaseRecord = inputRecords[0];
    Dictionary<string, List<int>> expectedSexAlternatives = new() { ["MALE"] = [155], ["FEMALE"] = [135] };
    Dictionary<string, List<int>> expectedExistingAlternatives = new() { ["BRACKET1"] = [50], ["BRACKET2"] = [70] };
    DemographicsDistributionRecord expectedExistingBaseRecord = new()
    {
      Collection = "sub_collection",
      Code = "EXISTING_CODE",
      Description = "TEST_DESCRIPTION",
      Alternatives = "^BRACKET1|50^BRACKET2|70^",
      Dataset = "Person",
      Category = "Demographic"
    };

    var actual = initialAccumulator.AccumulateData(inputRecords);

    // Here's what we added
    Assert.Equivalent(
      new AlternativesAccumulator(expectedSexBaseRecord) { Alternatives = expectedSexAlternatives },
      actual.Alternatives[Demographics.Sex]);

    // Here's what should remain unchanged
    Assert.Equivalent(
      new AlternativesAccumulator(expectedExistingBaseRecord) { Alternatives = expectedExistingAlternatives },
      actual.Alternatives["EXISTING_CODE"]);
  }

  [Fact]
  public void AccumulateData_WhenNewRowForExistingCode_CorrectAlternativesAccumulation()
  {
    // When a code is already present
    //   - base record remains as previous
    //   - alternatives are added to existing alternative keys
    //   - new alternatives keys are initialised // Not sure if this is technically legal in Task API but err on flexibility

    var initialAccumulator = _singleCodeAccumulator;

    List<DemographicsDistributionRecord> inputRecords =
    [
      new()
      {
        Code = "EXISTING_CODE", Collection = "sub_collection2",
        Alternatives = "^BRACKET2|155^BRACKET1|135^BRACKET3|504^"
      }
    ];

    // Much of our expected state has to be redefined manually due to mutable reference types
    Dictionary<string, List<int>> expectedAlternatives =
      new() { ["BRACKET1"] = [50, 135], ["BRACKET2"] = [70, 155], ["BRACKET3"] = [504] };
    DemographicsDistributionRecord expectedExistingBaseRecord = new()
    {
      Collection = "sub_collection",
      Code = "EXISTING_CODE",
      Description = "TEST_DESCRIPTION",
      Alternatives = "^BRACKET1|50^BRACKET2|70^",
      Dataset = "Person",
      Category = "Demographic"
    };

    var actual = initialAccumulator.AccumulateData(inputRecords);

    Assert.Equivalent(
      new AlternativesAccumulator(expectedExistingBaseRecord) { Alternatives = expectedAlternatives },
      actual.Alternatives["EXISTING_CODE"]);
  }

  [Fact]
  public void AccumulateData_WhenDuplicateCodeRows_CorrectAlternativesAccumulation()
  {
    // Duplicate Codes in the same Results File
    //   - are handled according to the same rules* - should be impossible but do it right anyway
    // * see `AccumulateData_WhenNewRowForExistingCode_CorrectAlternativesAccumulation`
    //    and `AccumulateData_WhenFirstRowForCode_CorrectAlternativesAccumulation`

    var initialAccumulator = _singleCodeAccumulator;

    List<DemographicsDistributionRecord> inputRecords =
    [
      new() { Code = "EXISTING_CODE", Collection = "sub_collection2", Alternatives = "^BRACKET2|155^BRACKET1|135^" },
      new() { Code = "EXISTING_CODE", Collection = "sub_collection2", Alternatives = "^BRACKET1|27^BRACKET2|32^" },
      new() { Code = Demographics.Sex, Collection = "sub_collection2", Alternatives = "^MALE|155^FEMALE|135^" },
      new() { Code = Demographics.Sex, Collection = "sub_collection2", Alternatives = "^MALE|42^FEMALE|68^OTHER|7^" }
    ];

    // Much of our expected state has to be redefined manually due to mutable reference types
    Dictionary<string, List<int>> expectedExistingAlternatives =
      new() { ["BRACKET1"] = [50, 135, 27], ["BRACKET2"] = [70, 155, 32] };
    Dictionary<string, List<int>> expectedSexAlternatives =
      new() { ["MALE"] = [155, 42], ["FEMALE"] = [135, 68], ["OTHER"] = [7] };

    var actual = initialAccumulator.AccumulateData(inputRecords);

    // New Code
    Assert.Equivalent(
      expectedSexAlternatives,
      actual.Alternatives[Demographics.Sex].Alternatives);

    // Existing Code
    Assert.Equivalent(
      expectedExistingAlternatives,
      actual.Alternatives["EXISTING_CODE"].Alternatives);
  }


  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void AccumulateData_WhenResultsIncludeAgeRecords_CorrectAlternativesAccumulation(bool ageOnly)
  {
    // TODO: When AGE aggregation support is added, revisit this
    // When AGE records are included in inputs
    //   - AGE never appears in the accumulated state (i.e. ignore AGE records)
    //   - Other records are accumulated as normal
    DemographicsAccumulator initialAccumulator = new("test_collection");

    DemographicsDistributionRecord ageRecord = new()
      { Collection = "sub_collection", Code = "AGE", Count = 16, Min = 9, Max = 25, Mean = 16.75, Median = 15.5 };

    DemographicsDistributionRecord sexRecord = new()
      { Code = Demographics.Sex, Collection = "sub_collection", Alternatives = "^MALE|155^FEMALE|135^" };

    List<DemographicsDistributionRecord> inputRecords = ageOnly ? [ageRecord] : [ageRecord, sexRecord];

    Dictionary<string, List<int>> expectedSexAlternatives =
      new() { ["MALE"] = [155], ["FEMALE"] = [135] };

    var actual = initialAccumulator.AccumulateData(inputRecords);

    // AGE never appears in the accumulated state (i.e. ignore AGE records)
    Assert.False(actual.Alternatives.ContainsKey("AGE"));

    if (!ageOnly)
    {
      // Other records are accumulated as normal
      Assert.Equivalent(expectedSexAlternatives, actual.Alternatives["SEX"].Alternatives);
    }
  }

  #endregion

  #region FinaliseAggregation

  // FinaliseAggregation
  //  Against each coded base record
  //    - retains key Base record values (Dataset, Category, (Description)...)
  //    - Sets correct Collection ID (Parent Task)
  //    - Sums Alternatives
  //    - Obfuscates Sum
  //    - Calculates final dependent values (e.g. Count)

  [Fact]
  public void FinaliseAggregation_CorrectBaseRecordProperties()
  {
    //    - retains key Base record values (Dataset, Category, (Description)...)
    //    - Sets correct Collection ID (Parent Task)
    var demographicsAccumulator = _multiCodeAccumulator;

    var actual = demographicsAccumulator.FinaliseAggregation();

    Assert.All(actual,
      x =>
      {
        // Base Record matches
        Assert.Equal(_multiCodeAccumulator.Alternatives[x.Code].BaseRecord.Description, x.Description);
        Assert.Equal(_multiCodeAccumulator.Alternatives[x.Code].BaseRecord.Dataset, x.Dataset);
        Assert.Equal(_multiCodeAccumulator.Alternatives[x.Code].BaseRecord.Category, x.Category);

        // Base Record changes
        Assert.Equal(_multiCodeAccumulator.CollectionId, x.Collection);
      });
  }

  [Fact]
  public void FinaliseAggregation_SumsAlternativesCorrectly()
  {
    var demographicsAccumulator = _multiCodeAccumulator;


    Dictionary<string, string> expectedAlternatives = new()
    {
      ["CODE1"] = "^BRACKET1|50^BRACKET2|150^",
      ["CODE2"] = "^BRACKET_A|81^BRACKET_B|557^BRACKET_C|32^"
    };
    
    // Pass an obfuscator explicitly with everything turned off
    // so it doesn't mess with our manual summing ;)
    var obfuscator = new Obfuscator(Options.Create(new ObfuscationOptions
      { RoundingTarget = 0, LowNumberSuppressionThreshold = 0 }));

    var actual = demographicsAccumulator.FinaliseAggregation(obfuscator);

    foreach (var record in actual)
    {
      Assert.Equal(expectedAlternatives[record.Code], record.Alternatives);
    }
  }

  [Fact]
  public void FinaliseAggregation_Obfuscator_CalledOncePerAlternativesKey()
  {
    var demographicsAccumulator = _multiCodeAccumulator;

    var obfuscator = new Mock<IObfuscator>();

    demographicsAccumulator.FinaliseAggregation(obfuscator.Object);

    obfuscator.Verify(
      x => x.Obfuscate(It.IsAny<int>()),
      Times.Exactly(_multiCodeAccumulator.Alternatives.Values
        .Select(x => x.Alternatives.Count)
        .Sum()));
  }

  [Fact]
  public void FinaliseAggregation_SetsCorrectRowCount()
  {
    var demographicsAccumulator = _multiCodeAccumulator;

    Dictionary<string, int> expectedCodeCounts = new()
    {
      ["CODE1"] = 200,
      ["CODE2"] = 670,
    };

    // Pass an obfuscator explicitly with everything turned off
    // so it doesn't mess with our manual summing ;)
    var obfuscator = new Obfuscator(Options.Create(new ObfuscationOptions
      { RoundingTarget = 0, LowNumberSuppressionThreshold = 0 }));

    var actual = demographicsAccumulator.FinaliseAggregation(obfuscator);

    Assert.All(actual, record => Assert.Equal(expectedCodeCounts[record.Code], record.Count));
  }

  #endregion

  #region Process

  [Fact]
  public void Process_WhenNoSubTasks_ReturnEmptyGenomicsResult()
  {
    var subTasks = new List<RelaySubTaskModel>();
    var collectionId = "test-collection";

    var expected = new QueryResult
    {
      Count = 1,
      Files = [
        new ResultFile
        {
          FileDescription = "demographics.distribution analysis results",
          FileName = ResultFileName.DemographicsDistribution,
        }.WithData([
          new DemographicsDistributionRecord()
          {
            Code = Demographics.Genomics,
            Description = "Genomics",
            Collection = collectionId,
            Count = 0,
            Alternatives = "^No|0^",
            Dataset = "person",
            Category = "Demographics",
          },
        ])
      ],
      DatasetCount = 1
    };

    var obfuscator = new Mock<IObfuscator>();

    var aggregator = new DemographicsDistributionAggregator(obfuscator.Object);

    var actual = aggregator.Process(collectionId, subTasks);

    Assert.Equivalent(expected, actual);
  }

  [Fact]
  public void Process_AggregatorObfuscator_CalledOncePerAlternativesKey()
  {
    // This mainly ensures that the obfuscator used in the aggregator instance
    // is the one used internally (i.e. with the correct configuration, not a different (e.g. default) instance)

    var subTasks = ProcessSubTasks;
    var collectionId = subTasks.First().RelayTask.Collection;

    // Unique Alternative Keys across all subtasks
    // Note this depends on the test data, manually calulated
    var expectedTimes = 4;

    var obfuscator = new Mock<IObfuscator>();

    var aggregator = new DemographicsDistributionAggregator(obfuscator.Object);
    aggregator.Process(collectionId, subTasks);

    obfuscator.Verify(x => x.Obfuscate(It.IsAny<int>()), Times.Exactly(expectedTimes));
  }

  [Fact]
  public void Process_ExpectedFinalResult()
  {
    var subTasks = ProcessSubTasks;
    var collectionId = subTasks.First().RelayTask.Collection;

    var expected = new QueryResult()
    {
      Count = 2,
      DatasetCount = 1,
      Files =
      [
        new ResultFile
        {
          FileDescription = "demographics.distribution analysis results",
          FileName = ResultFileName.DemographicsDistribution,
        }.WithData([
          new DemographicsDistributionRecord()
          {
            Code = Demographics.Genomics,
            Description = "Genomics",
            Collection = collectionId,
            Count = 2482,
            Alternatives = "^No|2326^Imputed whole genome data|156^",
            Dataset = "person",
            Category = "Demographics",
          },
          new DemographicsDistributionRecord()
          {
            Code = Demographics.Sex,
            Description = "Sex",
            Collection = collectionId,
            Count = 1580,
            Alternatives = "^MALE|790^FEMALE|790^",
            Dataset = "person",
            Category = "Demographics",
          },
        ])
      ]
    };

    var obfuscator = new Mock<IObfuscator>();
    obfuscator.Setup(x => x.Obfuscate(It.IsAny<int>()))
      .Returns((int value) => value);

    var aggregator = new DemographicsDistributionAggregator(obfuscator.Object);

    var actual = aggregator.Process(collectionId, subTasks);

    Assert.Equivalent(expected, actual);
  }

  #endregion
}
