using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Models;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services.IndividualsQueryServiceTests;

public class TryEnqueueDownstreamTests
{
  private static readonly IOptions<RelayBeaconOptions> _defaultOptions = Options.Create<RelayBeaconOptions>(new()
  {
    Enable = true
  });

  [Fact]
  public async Task TryEnqueueDownstream_BeaconDisabled_ReturnsFalseWithLogs()
  {
    var logger = new Mock<ILogger<IndividualsQueryService>>();

    var service = new IndividualsQueryService(
      logger.Object,
      Options.Create<RelayBeaconOptions>(new()),
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>());

    var actual = await service.TryEnqueueDownstream([]);

    Assert.False(actual);
    logger.Verify(
      x => x.Log<It.IsAnyType>( // Must use logger.Log<It.IsAnyType> to sub-out FormattedLogValues, the internal class
        LogLevel.Warning, // Match whichever log level you want here
        0, // EventId
        It.Is<It.IsAnyType>((o, t) => string.Equals(
          "GA4GH Beacon Functionality is disabled; Individuals query will not be queued.",
          o.ToString())), // The type here must match the `logger.Log<T>` type used above
        null, //It.IsAny<Exception>(), // Whatever exception may have been logged with it, change as needed.
        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), // The message formatter
      Times.Once);
  }

  [Fact]
  public async Task TryEnqueueDownstream_NoSubnodes_ReturnsFalseWithLogs()
  {
    var logger = new Mock<ILogger<IndividualsQueryService>>();

    var service = new IndividualsQueryService(
      logger.Object,
      _defaultOptions,
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>());

    var actual = await service.TryEnqueueDownstream(["OMOP:123", "OMOP:456"]);

    Assert.False(actual);
    logger.Verify(
      x => x.Log( // Must use logger.Log<It.IsAnyType> to sub-out FormattedLogValues, the internal class
        LogLevel.Error, // Match whichever log level you want here
        0, // EventId
        It.Is<It.IsAnyType>((o, t) => string.Equals(
          "No subnodes are configured. The requested GA4GH Beacon Individuals Query will not be queued.",
          o.ToString())), // The type here must match the `logger.Log<T>` type used above
        null, //It.IsAny<Exception>(), // Whatever exception may have been logged with it, change as needed.
        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), // The message formatter
      Times.Once);
  }

  [Fact]
  public async Task TryEnqueueDownstream_EmptyTermsList_ReturnsFalseWithLogs()
  {
    var logger = new Mock<ILogger<IndividualsQueryService>>();

    var service = new IndividualsQueryService(
      logger.Object,
      _defaultOptions,
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>());

    var actual = await service.TryEnqueueDownstream([]);

    Assert.False(actual);
    logger.Verify(
      x => x.Log<It.IsAnyType>( // Must use logger.Log<It.IsAnyType> to sub-out FormattedLogValues, the internal class
        LogLevel.Warning, // Match whichever log level you want here
        0, // EventId
        It.Is<It.IsAnyType>((o, t) => string.Equals(
          "GA4GH Beacon Individuals Query with no Filters will not be queued.",
          o.ToString())), // The type here must match the `logger.Log<T>` type used above
        null, //It.IsAny<Exception>(), // Whatever exception may have been logged with it, change as needed.
        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), // The message formatter
      Times.Once);
  }

  [Fact]
  public async Task TryEnqueueDownstream_ValidState_EnqueuesAndReturnsTrue()
  {
    var logger = new Mock<ILogger<IndividualsQueryService>>();
    var downstreamTasks = new Mock<IDownstreamTaskService>();
    
    var subnodes = new Mock<ISubNodeService>();
    subnodes.Setup(x => x.List())
      .Returns(() => Task.FromResult(
        new List<SubNodeModel>()
        {
          new()
          {
            Id = Guid.NewGuid(), Owner = "test"
          }
        }.AsEnumerable()));

    var service = new IndividualsQueryService(
      logger.Object,
      _defaultOptions,
      subnodes.Object,
      downstreamTasks.Object);

    var actual = await service.TryEnqueueDownstream(["OMOP:123", "OMOP:456"]);

    // Enqueues
    downstreamTasks.Verify(x =>
        x.Enqueue(It.IsAny<AvailabilityJob>(), It.IsAny<List<SubNodeModel>>()),
      Times.Once);

    // Returns true
    Assert.True(actual);

    // No warning logs
    logger.Verify(
      x => x.Log<It.IsAnyType>( // Must use logger.Log<It.IsAnyType> to sub-out FormattedLogValues, the internal class
        LogLevel.Warning, // Match whichever log level you want here
        0, // EventId
        It.IsAny<It.IsAnyType>(), // The type here must match the `logger.Log<T>` type used above
        null, //It.IsAny<Exception>(), // Whatever exception may have been logged with it, change as needed.
        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), // The message formatter
      Times.Never);

    // No error logs
    logger.Verify(
      x => x.Log<It.IsAnyType>( // Must use logger.Log<It.IsAnyType> to sub-out FormattedLogValues, the internal class
        LogLevel.Error, // Match whichever log level you want here
        0, // EventId
        It.IsAny<It.IsAnyType>(), // The type here must match the `logger.Log<T>` type used above
        null, //It.IsAny<Exception>(), // Whatever exception may have been logged with it, change as needed.
        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), // The message formatter
      Times.Never);
  }
}
