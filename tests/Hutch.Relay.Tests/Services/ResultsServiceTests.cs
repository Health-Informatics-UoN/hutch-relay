using Castle.Core.Logging;
using Hutch.Rackit;
using Hutch.Rackit.TaskApi.Contracts;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config;
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
  [InlineData(true)]
  [InlineData(false)]
  public async Task CompleteRelayTask_SubmitsResults_WhenUpstreamTaskApiIsEnabled(bool upstreamTaskApiIsEnabled)
  {
    var relayTask = new RelayTaskModel()
    {
      Id = Guid.NewGuid().ToString(),
      Collection = Guid.NewGuid().ToString(),
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
        x.Process(It.Is<string>(x => x == relayTask.Collection), It.IsAny<List<RelaySubTaskModel>>()))
      .Returns(() => new() { Count = 0 });

    var logger = Mock.Of<ILogger<ResultsService>>();

    var taskApi = new Mock<ITaskApiClient>();
    taskApi
      .Setup(x =>
        x.SubmitResultAsync(It.IsAny<string>(), It.IsAny<JobResult>(), It.IsAny<ApiClientOptions>()));

    TaskApiPollingOptions taskApiOptions = new()
    {
      Enable = upstreamTaskApiIsEnabled
    };

    var resultsService = new ResultsService(
      logger,
      Options.Create(taskApiOptions),
      Options.Create<DatabaseOptions>(new()),
      taskApi.Object,
      tasks.Object,
      aggregator.Object,
      aggregator.Object,
      aggregator.Object
    );

    // Act
    await resultsService.CompleteRelayTask(relayTask);

    // Assert
    taskApi.Verify(x =>
      x.SubmitResultAsync(It.IsAny<string>(), It.IsAny<JobResult>(), It.IsAny<ApiClientOptions>()),
      upstreamTaskApiIsEnabled ? Times.Once : Times.Never);
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

    var resultsService = new ResultsService(
      null!,
      Options.Create<TaskApiPollingOptions>(new()),
      Options.Create<DatabaseOptions>(new()),
      null!,
      tasks.Object,
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
}
