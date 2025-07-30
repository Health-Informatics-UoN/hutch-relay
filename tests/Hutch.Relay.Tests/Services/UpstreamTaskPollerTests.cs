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
  public async Task PollAllQueues_WithAvailabilityTask_EnqueuesDownstream()
  {
    var testPollingDuration = TimeSpan.FromSeconds(20);

    // Arrange
    var availabilityTask = new AvailabilityJob();
    
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
    var subnodes = new List<SubNodeModel>([new()
      {
        Id = Guid.NewGuid(),
        Owner = "user"
      }]);
    subNodes.Setup(x => x.List()).Returns(Task.FromResult(subnodes.AsEnumerable()));

    var queues = new Mock<IRelayTaskQueue>();
    queues.Setup(x =>
      x.IsReady(It.IsAny<string>())).Returns(Task.FromResult(true));

    var downstreamTasks = new Mock<IDownstreamTaskService>();

    // Setup a scope factory that mostly just resolves dependencies with our setup mocks
    var serviceScopeFactory = new ServiceCollection()
      .AddScoped<ScopedTaskHandler>()
      .AddScoped<ILogger<ScopedTaskHandler>>(x => LoggerFactory.Create(b => b.AddDebug()).CreateLogger<ScopedTaskHandler>())
      .AddScoped<IDownstreamTaskService>(x => downstreamTasks.Object)
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
    downstreamTasks.Verify(x => x.Enqueue(It.IsAny<AvailabilityJob>(), It.IsAny<List<SubNodeModel>>()), Times.Once);

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
