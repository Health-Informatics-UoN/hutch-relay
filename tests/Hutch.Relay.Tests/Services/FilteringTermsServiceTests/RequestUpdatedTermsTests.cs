using System.Data.Common;
using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Constants;
using Hutch.Relay.Data;
using Hutch.Relay.Models;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services.FilteringTermsServiceTests;

public class RequestUpdatedTermsTests : IDisposable
{
  private readonly DbConnection? _connection = null;

  private static readonly IOptions<RelayBeaconOptions> _defaultOptions = Options.Create<RelayBeaconOptions>(new()
  {
    Enable = true
  });

  private readonly ApplicationDbContext _dbContext;

  public RequestUpdatedTermsTests()
  {
    // Ensure a unique DB per Test
    _dbContext = FixtureHelpers.NewDbContext(ref _connection);
    _dbContext.Database.EnsureCreated();
  }

  public void Dispose()
  {
    _dbContext.Database.EnsureDeleted();
    _connection?.Dispose();
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public async Task RequestUpdatedTerms_BeaconDisabled_LogsWarningAndDoesNothing(bool isBeaconEnabled)
  {
    var logger = new Mock<ILogger<FilteringTermsService>>();

    var subNodes = new Mock<ISubNodeService>();
    var subnodes = new List<SubNodeModel>([new()
      {
        Id = Guid.NewGuid(),
        Owner = "user"
      }]);
    subNodes.Setup(x => x.List()).Returns(Task.FromResult(subnodes.AsEnumerable()));

    var downstreamTasks = new Mock<IDownstreamTaskService>();

    var filteringTermsService = new FilteringTermsService(
      logger.Object,
      Options.Create<RelayBeaconOptions>(new()
      {
        Enable = isBeaconEnabled
      }),
      subNodes.Object,
      downstreamTasks.Object,
      _dbContext,
      Mock.Of<IRelayTaskService>());

    await filteringTermsService.RequestUpdatedTerms();

    logger.Verify(
      x => x.Log<It.IsAnyType>( // Must use logger.Log<It.IsAnyType> to sub-out FormattedLogValues, the internal class
        LogLevel.Warning, // Match whichever log level you want here
        0, // EventId
        It.Is<It.IsAnyType>((o, t) => string.Equals(
          "GA4GH Beacon Functionality is disabled; not requesting updated Filtering Terms.", o.ToString())), // The type here must match the `logger.Log<T>` type used above
        null, //It.IsAny<Exception>(), // Whatever exception may have been logged with it, change as needed.
        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), // The message formatter
      isBeaconEnabled ? Times.Never : Times.Once);

    downstreamTasks.Verify(
    x => x.Enqueue(
      It.IsAny<TaskApiBaseResponse>(),
      It.IsAny<List<SubNodeModel>>()),
    isBeaconEnabled ? Times.Once : Times.Never);
  }

  [Fact]
  public async Task RequestUpdatedTerms_EnqueuesDownstream_BeaconCodeDistributionTask()
  {
    // Arrange
    var expectedTask = new CollectionAnalysisJob()
    {
      Analysis = AnalysisType.Distribution,
      Code = DistributionCode.Generic,
      Collection = RelayBeaconTaskDetails.Collection,
      Owner = RelayBeaconTaskDetails.Owner
    };

    var subNodes = new Mock<ISubNodeService>();
    var subnodes = new List<SubNodeModel>([new()
      {
        Id = Guid.NewGuid(),
        Owner = "user"
      }]);
    subNodes.Setup(x => x.List()).Returns(Task.FromResult(subnodes.AsEnumerable()));

    var logger = Mock.Of<ILogger<FilteringTermsService>>();
    var downstreamTasks = new Mock<IDownstreamTaskService>();

    var service = new FilteringTermsService(
      logger,
      _defaultOptions,
      subNodes.Object,
      downstreamTasks.Object,
      _dbContext,
      Mock.Of<IRelayTaskService>());

    // Act
    await service.RequestUpdatedTerms();

    // Assert
    downstreamTasks.Verify(
      x => x.Enqueue(
        It.Is<CollectionAnalysisJob>(x =>
          x.Uuid.StartsWith(RelayBeaconTaskDetails.IdPrefix) &&
          x.Analysis == expectedTask.Analysis &&
          x.Code == expectedTask.Code &&
          x.Collection == expectedTask.Collection &&
          x.Owner == expectedTask.Owner),
        It.IsAny<List<SubNodeModel>>()),
      Times.Once);
  }

  [Fact]
  public async Task RequestUpdatedTerms_WhenCodeDistributionTasksInProgress_LogsInformationAndReturns()
  {
    // Arrange
    var logger = new Mock<ILogger<FilteringTermsService>>();
    var downstreamTasks = new Mock<IDownstreamTaskService>();

    var relayTasks = new Mock<IRelayTaskService>();
    relayTasks.Setup(x => x.ListIncomplete())
      .Returns(() => Task.FromResult(
        new List<RelayTaskModel>()
        {
          new() { Collection = "", Id = "123", Type = TaskTypes.TaskApi_CodeDistribution }
        }.AsEnumerable()));

    var service = new FilteringTermsService(
      logger.Object,
      _defaultOptions,
      Mock.Of<ISubNodeService>(),
      downstreamTasks.Object,
      _dbContext,
      relayTasks.Object);

    // Act
    await service.RequestUpdatedTerms();

    // Assert
    logger.Verify(
      x => x.Log<It.IsAnyType>( // Must use logger.Log<It.IsAnyType> to sub-out FormattedLogValues, the internal class
        LogLevel.Information, // Match whichever log level you want here
        0, // EventId
        It.Is<It.IsAnyType>((o, t) => string.Equals(
          "Downstream Generic Code Distribution Tasks are already in progress; not requesting updated Filtering Terms.", o.ToString())), // The type here must match the `logger.Log<T>` type used above
        null, //It.IsAny<Exception>(), // Whatever exception may have been logged with it, change as needed.
        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), // The message formatter
      Times.Once);

    downstreamTasks.Verify(
      x => x.Enqueue(
        It.IsAny<TaskApiBaseResponse>(),
        It.IsAny<List<SubNodeModel>>()),
      Times.Never);
  }
}
