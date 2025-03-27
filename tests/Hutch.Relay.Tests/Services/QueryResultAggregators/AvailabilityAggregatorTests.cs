using System.Text.Json;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Constants;
using Hutch.Relay.Models;
using Hutch.Relay.Services;
using Hutch.Relay.Services.JobResultAggregators;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services.QueryResultAggregators;

public class AvailabilityAggregatorTests
{
  // We don't do in depth obfuscation tests here; they are done in the Obfuscator test suite.
  // But we do attempt to ensure that the Obfuscator is being applied to aggregate outputs

  [Fact]
  public void WhenNoSubTasks_ReturnEmptyQueryResult()
  {
    var subTasks = new List<RelaySubTaskModel>();

    var expected = new QueryResult
    {
      Count = 0,
      Files = [],
      DatasetCount = 0
    };

    var obfuscator = new Mock<IObfuscator>();

    var aggregator = new AvailabilityAggregator(obfuscator.Object);

    var actual = aggregator.Process(subTasks);

    Assert.Equivalent(expected, actual);
  }

  [Fact]
  public void ObfuscatorIsCalledOnce()
  {
    var subTasks = new List<RelaySubTaskModel>();

    var obfuscator = new Mock<IObfuscator>();

    var aggregator = new AvailabilityAggregator(obfuscator.Object);
    aggregator.Process(subTasks);

    obfuscator.Verify(x => x.Obfuscate(It.IsAny<int>()), Times.Once);
  }

  [Fact]
  public void WhenOneSubTask_AggregateCountIsUnchanged()
  {
    const int expectedCount = 50;

    var subTasks = new List<RelaySubTaskModel>()
    {
      new()
      {
        Id = Guid.NewGuid(),
        Owner = new() { Id = Guid.NewGuid(), Owner = "test" },
        RelayTask = new() { Id = "test", Type = TaskTypes.TaskApi_Availability, Collection = "test" },
        Result = JsonSerializer.Serialize(new JobResult
        {
          Results = new()
          {
            Count = expectedCount
          }
        })
      }
    };

    var expected = new QueryResult
    {
      Count = expectedCount,
      Files = [],
      DatasetCount = 0
    };

    var obfuscator = new Mock<IObfuscator>();
    obfuscator.Setup(x => x.Obfuscate(It.IsAny<int>()))
      .Returns(() => expectedCount);

    var aggregator = new AvailabilityAggregator(obfuscator.Object);

    var actual = aggregator.Process(subTasks);

    Assert.Equivalent(expected, actual);
  }

  [Theory]
  [InlineData(new[] { 3, 4, 5 })]
  [InlineData(new[] { 1, 2, 3, 4, 5 })]
  [InlineData(new[] { 42 })]
  [InlineData(new[] { 70, 431, 5423652, 3214, 654, 3213, 5342, 54, 65476, 76547, 234, 2, 5532 })]
  public void WhenSubTasks_AggregateCountIsSum(int[] subtaskCounts)
  {
    var expectedCount = subtaskCounts.Sum();

    var subTasks = subtaskCounts
      .Select(count =>
        new RelaySubTaskModel
        {
          Id = Guid.NewGuid(),
          Owner = new() { Id = Guid.NewGuid(), Owner = "test" },
          RelayTask = new() { Id = "test", Type = TaskTypes.TaskApi_DemographicsDistribution, Collection = "test" },
          Result = JsonSerializer.Serialize(new JobResult
          {
            Results = new()
            {
              Count = count
            }
          })
        })
      .ToList();

    var expected = new QueryResult
    {
      Count = expectedCount,
      Files = [],
      DatasetCount = 0
    };

    var obfuscator = new Mock<IObfuscator>();
    obfuscator.Setup(x => x.Obfuscate(It.IsAny<int>()))
      .Returns(() => expectedCount);

    var aggregator = new AvailabilityAggregator(obfuscator.Object);

    var actual = aggregator.Process(subTasks);

    Assert.Equivalent(expected, actual);
  }
}
