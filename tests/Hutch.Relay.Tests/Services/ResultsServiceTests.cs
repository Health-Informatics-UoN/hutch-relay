using Hutch.Rackit;
using Hutch.Rackit.TaskApi.Contracts;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Constants;
using Hutch.Relay.Models;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Hutch.Relay.Services.JobResultAggregators;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services;

public class ResultsServiceTests
{
  [Theory]
  [InlineData(true, true)]
  [InlineData(false, true)]
  [InlineData(true, false)]
  [InlineData(false, false)]
  public async Task CompleteRelayTask_SubmitsResults_WhenUpstreamTaskApiIsEnabledAndCollectionMatches(bool isUpstreamTaskApiEnabled, bool matchCollections)
  {
    var taskCollection = Guid.NewGuid().ToString();
    var configuredCollection = matchCollections ? taskCollection : Guid.NewGuid().ToString();

    var relayTask = new RelayTaskModel()
    {
      Id = Guid.NewGuid().ToString(),
      Collection = taskCollection,
      Type = TaskTypes.TaskApi_Availability,
    };

    var tasks = new Mock<IRelayTaskService>();
    tasks.Setup(x =>
        x.ListSubTasks(
          It.Is<string>(y => y == relayTask.Id),
          It.Is<bool>(y => y == true)))
      .Returns(() => Task.FromResult<IEnumerable<RelaySubTaskModel>>([]));

    var aggregator = new Mock<IQueryResultAggregator>();
    aggregator
      .Setup(x =>
        x.Process(It.Is<string>(x => x == taskCollection), It.IsAny<List<RelaySubTaskModel>>()))
      .Returns(() => new() { Count = 0 });

    var logger = Mock.Of<ILogger<ResultsService>>();

    var taskApi = new Mock<ITaskApiClient>();

    TaskApiPollingOptions taskApiOptions = new()
    {
      Enable = isUpstreamTaskApiEnabled,
      CollectionId = configuredCollection
    };

    var filteringTerms = Mock.Of<IFilteringTermsService>();

    var resultsService = new ResultsService(
      logger,
      Options.Create(taskApiOptions),
      Options.Create<RelayBeaconOptions>(new()),
      Options.Create<DatabaseOptions>(new()),
      taskApi.Object,
      tasks.Object,
      filteringTerms,
      aggregator.Object,
      aggregator.Object,
      aggregator.Object
    );

    // Act
    await resultsService.CompleteRelayTask(relayTask);

    // Assert
    taskApi.Verify(x =>
      x.SubmitResultAsync(It.IsAny<string>(), It.IsAny<JobResult>(), It.IsAny<ApiClientOptions>()),
      isUpstreamTaskApiEnabled && matchCollections ? Times.Once : Times.Never);
  }

  [Fact]
  public async Task PrepareFinalJobResult_SetsCorrectBaseProperties()
  {
    // Note that we don't test aggregation or obfuscation outputs here;
    // they are done in their respective service test suites

    var relayTask = new RelayTaskModel()
    {
      Id = Guid.NewGuid().ToString(),
      Collection = Guid.NewGuid().ToString(),
      Type = TaskTypes.TaskApi_Availability,
    };

    var expected = new JobResult()
    {
      Uuid = relayTask.Id,
      CollectionId = relayTask.Collection,
    };

    var tasks = new Mock<IRelayTaskService>();
    tasks.Setup(x =>
        x.ListSubTasks(
          It.Is<string>(y => y == relayTask.Id),
          It.Is<bool>(y => y == true)))
      .Returns(() => Task.FromResult<IEnumerable<RelaySubTaskModel>>([]));

    var aggregator = new Mock<IQueryResultAggregator>();
    aggregator
      .Setup(x =>
        x.Process(It.Is<string>(x => x == relayTask.Collection), It.IsAny<List<RelaySubTaskModel>>()))
      .Returns(() => new() { Count = 0 });

    var filteringTerms = Mock.Of<IFilteringTermsService>();

    var resultsService = new ResultsService(
      null!,
      Options.Create<TaskApiPollingOptions>(new()),
      Options.Create<RelayBeaconOptions>(new()),
      Options.Create<DatabaseOptions>(new()),
      null!,
      tasks.Object,
      filteringTerms,
      aggregator.Object,
      aggregator.Object,
      aggregator.Object
    );

    var actual = await resultsService.PrepareFinalJobResult(relayTask);

    // Check the relevant base properties
    Assert.Equal(expected.Uuid, actual.Uuid);
    Assert.Equal(expected.CollectionId, actual.CollectionId);
    Assert.Equal(expected.Message, actual.Message);
    Assert.Equal(expected.ProtocolVersion, actual.ProtocolVersion);
    Assert.Equal(expected.Status, actual.Status);
  }

  [Theory]
  [InlineData(true, TaskTypes.TaskApi_CodeDistribution)]
  [InlineData(false, TaskTypes.TaskApi_CodeDistribution)]
  [InlineData(true, TaskTypes.TaskApi_Availability)]
  [InlineData(false, TaskTypes.TaskApi_Availability)]
  [InlineData(true, TaskTypes.TaskApi_DemographicsDistribution)]
  [InlineData(false, TaskTypes.TaskApi_DemographicsDistribution)]

  public async Task CompleteRelayTask_WhenBeaconEnabledAndCodeDistributionResult_CachesFilteringTerms(bool isBeaconEnabled, string taskType)
  {
    var relayTask = new RelayTaskModel()
    {
      Id = Guid.NewGuid().ToString(),
      Collection = Guid.NewGuid().ToString(),
      Type = taskType,
    };

    var tasks = new Mock<IRelayTaskService>();
    tasks.Setup(x =>
        x.ListSubTasks(
          It.Is<string>(y => y == relayTask.Id),
          It.Is<bool>(y => y == true)))
      .Returns(() => Task.FromResult<IEnumerable<RelaySubTaskModel>>([]));

    var aggregator = new Mock<IQueryResultAggregator>();
    aggregator
      .Setup(x =>
        x.Process(It.IsAny<string>(), It.IsAny<List<RelaySubTaskModel>>()))
      .Returns(() => new() { Count = 0 });

    var logger = Mock.Of<ILogger<ResultsService>>();

    var filteringTerms = new Mock<IFilteringTermsService>();

    RelayBeaconOptions beaconOptions = new()
    {
      Enable = isBeaconEnabled
    };

    var taskApi = Mock.Of<ITaskApiClient>();

    var resultsService = new ResultsService(
      logger,
      Options.Create<TaskApiPollingOptions>(new()),
      Options.Create(beaconOptions),
      Options.Create<DatabaseOptions>(new()),
      taskApi,
      tasks.Object,
      filteringTerms.Object,
      aggregator.Object,
      aggregator.Object,
      aggregator.Object
    );

    // Act
    await resultsService.CompleteRelayTask(relayTask);

    // Assert
    filteringTerms.Verify(x =>
      x.CacheUpdatedTerms(It.IsAny<JobResult>()),
      isBeaconEnabled && taskType == TaskTypes.TaskApi_CodeDistribution ? Times.Once : Times.Never);
  }
}
