using Hutch.Rackit.TaskApi.Models;
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
}
