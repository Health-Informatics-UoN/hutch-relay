using System.Runtime.InteropServices;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Models;
using Hutch.Relay.Services.JobResultAggregators;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xunit;

namespace Hutch.Relay.Tests.Services.QueryResultAggregators;

public class DemographicsDistributionAggregatorTests
{
  // Later testing
  //   - Test that the validated accumulator correctly finalises into expected outputs for a given input
  //      - Test the Finaliser method(s)
  //      - Test `Process()` end-to-end much like CodeDistribution does today

  #region AccumulateData

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
          Alternatives = "^BRACKET1|50^BRACKET2|70^"
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

  [Fact]
  public void AccumulateData_WhenFirstRowForCode_CorrectAlternativesAccumulation()
  {
    // When a code isn't already in the accumulated state:
    //   - base record is set to the record in question
    //   - alternatives are initialised with values from the record in question

    var initialAccumulator = _singleCodeAccumulator;

    List<DemographicsDistributionRecord> inputRecords =
    [
      new() { Code = "SEX", Collection = "sub_collection2", Alternatives = "^MALE|155^FEMALE|135^" }
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
      Alternatives = "^BRACKET1|50^BRACKET2|70^"
    };

    var actual = initialAccumulator.AccumulateData(inputRecords);

    // Here's what we added
    Assert.Equivalent(
      new AlternativesAccumulator(expectedSexBaseRecord) { Alternatives = expectedSexAlternatives },
      actual.Alternatives["SEX"]);

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
      Alternatives = "^BRACKET1|50^BRACKET2|70^"
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
      new() { Code = "SEX", Collection = "sub_collection2", Alternatives = "^MALE|155^FEMALE|135^" },
      new() { Code = "SEX", Collection = "sub_collection2", Alternatives = "^MALE|42^FEMALE|68^OTHER|7^" }
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
      actual.Alternatives["SEX"].Alternatives);

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
      { Code = "SEX", Collection = "sub_collection", Alternatives = "^MALE|155^FEMALE|135^" };

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
}
