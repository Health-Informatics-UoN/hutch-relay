using System.Runtime.CompilerServices;
using Hutch.Rackit;
using Hutch.Rackit.TaskApi.Contracts;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Models;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services;

public class UpstreamTaskPollerTests()
{
  private readonly ILogger<UpstreamTaskPoller> _logger =
    LoggerFactory.Create(b => b.AddDebug()).CreateLogger<UpstreamTaskPoller>();

  [Fact]
  public async Task PollAllQueues_WithInvalidQueueConfig_ThrowsInvalidOperation()
  {
    // Arrange
    var options = Options.Create<ApiClientOptions>(new());

    var queues = new Mock<IRelayTaskQueue>();
    queues.Setup(x =>
      x.IsReady(It.IsAny<string>())).Returns(Task.FromResult(false));

    var poller = new UpstreamTaskPoller(_logger, options, null!, null!, null!, queues.Object);

    // Act, Assert
    await Assert.ThrowsAsync<InvalidOperationException>(async () => await poller.PollAllQueues(new()));
  }

  [Fact]
  public async Task PollAllQueues_WithAvailabilityTask_CreatesStateAndQueues()
  {
    var testPollingDuration = TimeSpan.FromSeconds(20);

    // Arrange
    var availabilityTask = new AvailabilityJob();
    var relayTask = new RelayTaskModel()
    {
      Id = Guid.NewGuid().ToString(),
      Collection = "test",
    };
    var relaySubTask = new RelaySubTaskModel()
    {
      Id = Guid.NewGuid(),
      Owner = new()
      {
        Id = Guid.NewGuid(),
        Owner = "user"
      },
      RelayTask = relayTask
    };

    var upstream = new Mock<ITaskApiClient>();
    var cts = new CancellationTokenSource();
    upstream.Setup(x =>
        x.PollJobQueue<AvailabilityJob>(It.IsAny<ApiClientOptions?>(), It.IsAny<CancellationToken>()))
      .Returns(SimulatePolling(cts.Token, availabilityTask));
    upstream.Setup(x =>
        x.PollJobQueue<CollectionAnalysisJob>(It.IsAny<ApiClientOptions?>(), It.IsAny<CancellationToken>()))
      .Returns(SimulatePolling<CollectionAnalysisJob>(cts.Token));

    var options = Options.Create<ApiClientOptions>(new());

    var subNodes = new Mock<ISubNodeService>();
    subNodes.Setup(x => x.List()).Returns(Task.FromResult<IEnumerable<SubNodeModel>>([relaySubTask.Owner]));

    var tasks = new Mock<IRelayTaskService>();
    var taskDb = new List<RelayTaskModel>();
    tasks.Setup(x =>
        x.Create(It.IsAny<RelayTaskModel>()))
      .Returns(() =>
      {
        taskDb.Add(relayTask);
        return Task.FromResult(relayTask);
      });

    var subtaskDb = new List<RelaySubTaskModel>();
    tasks.Setup(x =>
        x.CreateSubTask(relayTask.Id, relaySubTask.Owner.Id))
      .Returns(() =>
      {
        subtaskDb.Add(relaySubTask);
        return Task.FromResult(relaySubTask);
      });

    var queues = new Mock<IRelayTaskQueue>();
    var queue = new List<AvailabilityJob>();
    queues.Setup(x =>
      x.IsReady(It.IsAny<string>())).Returns(Task.FromResult(true));
    queues.Setup(x => x.Send(relaySubTask.Owner.Id.ToString(), availabilityTask)).Returns(() =>
    {
      queue.Add(availabilityTask);
      return Task.CompletedTask;
    });

    var poller = new UpstreamTaskPoller(_logger, options, upstream.Object, subNodes.Object, tasks.Object, queues.Object);

    // Act
    // set a timer to cancel polling after a few
    var timer = new System.Timers.Timer(testPollingDuration)
    {
      AutoReset = false
    };
    timer.Elapsed += (s, e) =>
    {
      cts.Cancel();
      cts.Dispose();
    };
    timer.Start();

    await poller.PollAllQueues(cts.Token);

    // Assert
    Assert.Multiple(() =>
    {
      Assert.Single(taskDb);
      Assert.Single(subtaskDb);
      Assert.Single(queue);

      Assert.Contains(relayTask, taskDb);
      Assert.Contains(relaySubTask, subtaskDb);
      Assert.Contains(availabilityTask, queue);
    });

    return;

    static async IAsyncEnumerable<T> SimulatePolling<T>([EnumeratorCancellation] CancellationToken ct,
      T? firstResponse = null)
      where T : TaskApiBaseResponse
    {
      if (firstResponse is not null) yield return firstResponse;
      while (true)
      {
        await Task.Delay(5000, ct);
      }
    }
  }
}
