using System.Runtime.CompilerServices;
using Hutch.Rackit;
using Hutch.Rackit.TaskApi.Contracts;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config;
using Hutch.Relay.Constants;
using Hutch.Relay.Models;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services;

public class UpstreamTaskPollerTests()
{
  private readonly ILogger<UpstreamTaskPoller> _logger =
    LoggerFactory.Create(b => b.AddDebug()).CreateLogger<UpstreamTaskPoller>();

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public async Task PollAllQueues_WhenUpstreamTaskApiDisabled_NoPolling(bool isUpstreamTaskApiEnabled)
  {
    // We "prove" this by spying on the queue check
    // and seeing that we don't even get that far if disabled

    // Arrange
    var options = Options.Create<TaskApiPollingOptions>(new() { Enable = isUpstreamTaskApiEnabled });

    var queues = new Mock<IRelayTaskQueue>();
    queues.Setup(x =>
      x.IsReady(It.IsAny<string>())).Returns(Task.FromResult(false));

    var poller = new UpstreamTaskPoller(_logger, options, null!, null!, queues.Object, null!);

    try
    {
      await poller.PollAllQueues(new());
    }
    catch { /* Just swallowing the intentional exception if we're enabled but missing queue config */ }
    finally
    {
      // Assert
      queues.Verify(x =>
        x.IsReady(It.IsAny<string>()),
        isUpstreamTaskApiEnabled ? Times.Once : Times.Never);
    }
  }

  [Fact]
  public async Task PollAllQueues_WithInvalidQueueConfig_ThrowsInvalidOperation()
  {
    // Arrange
    var options = Options.Create<TaskApiPollingOptions>(new());

    var queues = new Mock<IRelayTaskQueue>();
    queues.Setup(x =>
      x.IsReady(It.IsAny<string>())).Returns(Task.FromResult(false));

    var poller = new UpstreamTaskPoller(_logger, options, null!, null!, queues.Object, null!);

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
      Type = TaskTypes.TaskApi_Availability,
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

    var options = Options.Create<TaskApiPollingOptions>(new());

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

    // Setup a scope factory that mostly just resolves dependencies with our setup mocks
    var serviceScopeFactory = new ServiceCollection()
      .AddScoped<ScopedTaskHandler>()
      .AddScoped<ILogger<ScopedTaskHandler>>(x => LoggerFactory.Create(b => b.AddDebug()).CreateLogger<ScopedTaskHandler>())
      .AddScoped<IRelayTaskService>(x => tasks.Object)
      .AddScoped<IRelayTaskQueue>(x => queues.Object)
      .AddScoped<ISubNodeService>(x => subNodes.Object)
     .BuildServiceProvider()
     .GetRequiredService<IServiceScopeFactory>();

    var poller = new UpstreamTaskPoller(_logger, options, upstream.Object, subNodes.Object, queues.Object, serviceScopeFactory);

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

    try
    {
      await poller.PollAllQueues(cts.Token);
    }
    catch (OperationCanceledException)
    {
      // Expected
    }

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
