using Hutch.Rackit;
using Hutch.Rackit.TaskApi.Contracts;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config;
using Hutch.Relay.Extensions;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Services;

/// <summary>
/// For polling for tasks from an upstream system (e.g. Relay or BC|RQuest)
/// </summary>
public class UpstreamTaskPoller(
  ILogger<UpstreamTaskPoller> logger,
  IOptions<TaskApiPollingOptions> options,
  ITaskApiClient upstreamTasks,
  ISubNodeService subNodes,
  IRelayTaskQueue queues,
  IServiceScopeFactory serviceScopeFactory)
{
  public async Task PollAllQueues(CancellationToken stoppingToken)
  {
    var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

    // We need to simultaneously poll against all supported task queues in an upstream system
    // TODO: this may become configurable to support upstream unified queues e.g. in Relay

    // Test Queue Backend availability
    if (!await queues.IsReady())
      throw new InvalidOperationException(
        "The RelayTask Queue Backend is not ready; please check the logs and your configuration.");

    // setup polling threads for each job type // TODO: This should be configurable
    List<Task> pollers = [
      // Passing the same CancellationTokenSource allows us to cancel all the polling tasks when one of them fails
      PollJobQueue<AvailabilityJob>(cts),
      PollJobQueue<CollectionAnalysisJob>(cts)
      
      // TODO: Type C, D
    ];

    // start parallel polling threads
    await Task.WhenAll(pollers);
  }

  private async Task PollJobQueue<T>(CancellationTokenSource cts)
    where T : TaskApiBaseResponse, new()
  {
    var jobs = upstreamTasks.PollJobQueue<T>(options.Value, cts.Token);

    while (cts.Token.IsCancellationRequested == false)
    {
      try
      {
        // Check for subnodes before we even start polling,
        // to avoid pulling jobs and losing them / making http requests with no purpose
        subNodes.EnsureSubNodes(); // Though this uses the parent's scoped db context and we poll on different threads, it is an untracked read operation

        await foreach (var job in jobs.WithCancellation(cts.Token))
        {
          if (job is not null)
          {
            using var scope = serviceScopeFactory.CreateScope(); // create a new scope for each job (similar to aspnet per-request scope)
            var handler = scope.ServiceProvider.GetRequiredService<ScopedTaskHandler>(); // this will therefore get a scoped DbContext
            await handler.HandleTask(job); // handle the task in scope on its own thread
          }
        }
      }
      catch (Exception e)
      {
        // By default, exceptions should terminate; preferable in e.g. container environments
        // unless configured otherwise, or for gracefully handled specific cases

        // Always log
        logger.LogError(e,
          "An error occurred handling '{TypeName}' tasks.",
          typeof(T).Name);

        if (e.LogLevel() != LogLevel.Critical && options.Value.ResumeOnError)
        {
          // Swallow non-critical exceptions and just log; the while loop will restart polling
          var delayTime = TimeSpan.FromSeconds(options.Value.ErrorDelay);

          logger.LogInformation(e,
            "Waiting {DelaySeconds}s before resuming polling.",
            Math.Floor(delayTime.TotalSeconds));

          // TODO: maintain an exception limit that eventually DOES quit?

          // Delay before resuming the loop
          await Task
            .Delay(delayTime, cts.Token)
            // Stop this Task from throwing when cancelled
            .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);
        }
        else
        {
          cts.Cancel(); // cancel any other polling tasks too
          throw;
        }
      }
    }
  }
}

// Not a true dependency, as we service locate it within a controlled scope rather than inject it traditionally.
// This allows us to control the scope of the real dependencies which depend on the dbcontext,
// enforcing the context's scope correctly (i.e. per found task) instead of using just the poller's context across all (threaded!) tasks.
public class ScopedTaskHandler(
  ILogger<ScopedTaskHandler> logger,
  IRelayTaskQueue queues,
  ISubNodeService subNodes,
  IRelayTaskService relayTasks)
{
  public async Task HandleTask<T>(T job)
    where T : TaskApiBaseResponse
  {
    logger.LogInformation("Task retrieved: ({Type}) {Id}", typeof(T).Name, job.Uuid);

    // Get up-to-date Sub Nodes list
    var subnodes = (await subNodes.List()).ToList();
    // Make sure there still are some; leave the loop if not
    if (subnodes.Count == 0) return;

    // Create a parent task
    var relayTask = await relayTasks.Create(new()
    {
      Id = job.Uuid,
      Type = IRelayTaskService.GetTaskApiType(job),
      Collection = job.Collection
    });

    // Fan out to subtasks
    foreach (var subnode in subnodes)
    {
      var subTask = await relayTasks.CreateSubTask(relayTask.Id, subnode.Id);

      // Update the job for the target subnode
      job.Uuid = subTask.Id.ToString();
      job.Collection = subnode.Id.ToString();
      job.Owner = subnode.Owner;

      // Queue the task for the subnode
      await queues.Send(subnode.Id.ToString(), job);
    }
  }
}