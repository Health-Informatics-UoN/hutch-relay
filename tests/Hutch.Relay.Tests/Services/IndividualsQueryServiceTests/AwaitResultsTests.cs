using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Models.Beacon;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services.IndividualsQueryServiceTests;

public class AwaitResultsTests
{
  [Theory]
  [InlineData("")]
  [InlineData("      ")]
  public async Task AwaitResults_InvalidQueueName_ThrowsArgumentException(string queueName)
  {
    var service = new IndividualsQueryService(
      Mock.Of<ILogger<IndividualsQueryService>>(),
      Options.Create<RelayBeaconOptions>(new()),
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>(),
      Mock.Of<IBeaconResultsQueue>());

    await Assert.ThrowsAsync<ArgumentException>(async () => await service.AwaitResults(queueName));
  }

  [Fact]
  public async Task AwaitResults_ResultsInQueue_ReturnsResults()
  {
    const string queueName = "test-queue";
    const int count = 123;
    var expected = new IndividualsResponseSummary()
    {
      Exists = true,
      NumTotalResults = count
    };

    var resultsQueue = new Mock<IBeaconResultsQueue>();
    resultsQueue.Setup(x => x.AwaitResults(It.Is<string>(queueName, StringComparer.InvariantCulture)))
      .Returns(() => Task.FromResult(count));

    var service = new IndividualsQueryService(
      Mock.Of<ILogger<IndividualsQueryService>>(),
      Options.Create<RelayBeaconOptions>(new()
      {
        Enable = true,
        SecurityAttributes = { DefaultGranularity = Granularity.count }
      }),
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>(),
      resultsQueue.Object);

    var actual = await service.AwaitResults(queueName);

    Assert.Equivalent(expected, actual);
  }
}
