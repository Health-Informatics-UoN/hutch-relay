using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Constants;
using Hutch.Relay.Models;
using Hutch.Relay.Services;
using Hutch.Relay.Services.JobResultAggregators;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services.QueryResultAggregators;

public class GenericDistributionAggregatorTests
{
  // We don't do in depth obfuscation tests here; they are done in the Obfuscator test suite.
  // But we do attempt to ensure that the Obfuscator is being applied to aggregate outputs

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
        Type = TaskTypes.TaskApi_CodeDistribution,
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
              .WithAnalysisFileName(AnalysisType.Distribution, DistributionCode.Generic)
              .WithData(data)
          ]
        }
      })
    };
  }

  private static List<GenericDistributionRecord> GenerateSubTaskResults(
    string collectionId, int[] counts, int codeOffset = 1)
    => [.. counts.Select((v, i) => new GenericDistributionRecord
    {
      Code = $"CODE{i + codeOffset}",
      Collection = collectionId,
      Count = v,
    })];

  public static readonly RelaySubTaskModel SubTaskNoData = GenerateSubTaskWithResultsData([]);

  public static readonly RelaySubTaskModel SubTaskZeroCounts =
    GenerateSubTaskWithResultsData(GenerateSubTaskResults(
      Guid.NewGuid().ToString(),
      [0, 0, 0]));

  public static readonly RelaySubTaskModel SubTaskWithCounts1 =
    GenerateSubTaskWithResultsData(GenerateSubTaskResults(
      Guid.NewGuid().ToString(),
      [91, 230, 1342, 17]));

  public static readonly RelaySubTaskModel SubTaskWithCounts2 =
    GenerateSubTaskWithResultsData(GenerateSubTaskResults(
      Guid.NewGuid().ToString(),
      [412, 0, 975, 26],
      3));

  public static IEnumerable<object[]> GetSubTasks()
  {
    yield return [new List<RelaySubTaskModel>(), 0, new List<int>()];
    yield return [new List<RelaySubTaskModel> { SubTaskNoData }, 0, new List<int>()];
    yield return [new List<RelaySubTaskModel> { SubTaskZeroCounts }, 3, new List<int> { 0, 0, 0 }];
    yield return [new List<RelaySubTaskModel> { SubTaskWithCounts1 }, 4, new List<int> { 91, 230, 1342, 17 }];
    yield return
    [
      new List<RelaySubTaskModel> { SubTaskZeroCounts, SubTaskWithCounts1, SubTaskWithCounts2 }, 6,
      new List<int> { 91, 230, 1754, 17, 975, 26 }
    ];
  }

  #endregion

  [Fact]
  public void WhenNoSubTasks_ReturnEmptyQueryResult()
  {
    var collectionId = "test-collection";
    var subTasks = new List<RelaySubTaskModel>();

    var expected = new QueryResult
    {
      Count = 0,
      Files = [],
      DatasetCount = 0
    };

    var obfuscator = new Mock<IObfuscator>();

    var aggregator = new GenericDistributionAggregator(
      obfuscator.Object,
      Mock.Of<ILogger<GenericDistributionAggregator>>());

    var actual = aggregator.Process(collectionId, subTasks);

    Assert.Equivalent(expected, actual);
  }

  [Theory]
  [MemberData(nameof(GetSubTasks))]
  public void ObfuscatorIsCalledOncePerAggregatedFileRow(List<RelaySubTaskModel> subTasks, int aggregatedRowCount,
    List<int> _) // expectedAggregates is not used in this test
  {
    var collectionId = subTasks.FirstOrDefault()?.RelayTask.Collection ?? "test-collection";

    var obfuscator = new Mock<IObfuscator>();

    var aggregator = new GenericDistributionAggregator(
      obfuscator.Object,
      Mock.Of<ILogger<GenericDistributionAggregator>>());
    aggregator.Process(collectionId, subTasks);

    obfuscator.Verify(x => x.Obfuscate(It.IsAny<int>()), Times.Exactly(aggregatedRowCount));
  }

  [Theory]
  [MemberData(nameof(GetSubTasks))]
  public void ReturnsCorrectPropertiesAndAggregates(List<RelaySubTaskModel> subTasks,
    int aggregatedRowCount,
    List<int> expectedAggregates)
  {
    var collectionId = subTasks.FirstOrDefault()?.RelayTask.Collection ?? "test-collection";

    var obfuscator = new Mock<IObfuscator>();
    obfuscator.Setup(x => x.Obfuscate(It.IsAny<int>()))
      .Returns((int value) => value);

    var aggregator = new GenericDistributionAggregator(
      obfuscator.Object,
      Mock.Of<ILogger<GenericDistributionAggregator>>());

    var actual = aggregator.Process(collectionId, subTasks);

    // Check the count fields
    Assert.Equal(aggregatedRowCount, actual.Count);
    if (aggregatedRowCount + expectedAggregates.Count == 0)
    {
      // If zero rows, should be no files
      Assert.Equal(0, actual.DatasetCount);
      Assert.Empty(actual.Files);
      return;
    }

    // With results there should only be 1 file
    Assert.Equal(1, actual.DatasetCount);
    Assert.Single(actual.Files);

    // If we have results, parse the result ourselves for assertion
    var decodedFileResult = actual.Files.Single().DecodeData();
    var config = CsvConfiguration.FromAttributes<GenericDistributionRecord>();
    config.MissingFieldFound = null;
    using var reader = new StringReader(decodedFileResult);
    using var csv = new CsvReader(reader, config);
    var rowsByCode = csv.GetRecords<GenericDistributionRecord>()
      .ToDictionary(x => x.Code, x => (aggregate: x.Count, collection: x.Collection));

    // Check the row count matches what's expected and what's described
    Assert.Equal(expectedAggregates.Count, rowsByCode.Count);
    Assert.Equal(actual.Count, rowsByCode.Count);

    // Check each row's count and collection
    for (var i = 0; i < expectedAggregates.Count; i++)
    {
      var expected = expectedAggregates[i];
      var code = $"CODE{i + 1}";

      Assert.Equal(expected, rowsByCode[code].aggregate);
      Assert.Equal(subTasks.First().RelayTask.Collection, rowsByCode[code].collection);
    }
  }
}
