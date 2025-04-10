using Hutch.Rackit;
using Hutch.Rackit.TaskApi.Contracts;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Services;

/// <summary>
/// This is a background worker (IHostedService) for polling for tasks from an upstream system (e.g. Relay or BC|RQuest)
/// </summary>
public class UpstreamTaskPoller(
  ILogger<UpstreamTaskPoller> logger,
  IOptions<ApiClientOptions> options,
  ITaskApiClient upstreamTasks,
  ISubNodeService subNodes,
  IRelayTaskService relayTasks,
  IRelayTaskQueue queues)
{
  public async Task PollAllQueues(CancellationToken stoppingToken)
  {
    // We need to simultaneously poll against all supported task queues in an upstream system
    // TODO: this may become configurable to support upstream unified queues e.g. in Relay

    // Test Queue Backend availability
    if (!await queues.IsReady())
      throw new InvalidOperationException(
        "The RelayTask Queue Backend is not ready; please check the logs and your configuration.");

    // Start polling for job types: // TODO: Should this be configurable?
    var availabilityQueries =
      upstreamTasks.PollJobQueue<AvailabilityJob>(options.Value, stoppingToken);
    var collectionAnalyses =
      upstreamTasks.PollJobQueue<CollectionAnalysisJob>(options.Value, stoppingToken);

    // TODO: "Type C" var cohortAnalyses = upstreamTasks.PollJobQueue<>(options.Value, stoppingToken);


    // start parallel handler threads
    await Task.WhenAll(
      HandleTasksFound(availabilityQueries, stoppingToken),
      HandleTasksFound(collectionAnalyses, stoppingToken));
  }

  private async Task HandleTasksFound<T>(IAsyncEnumerable<T> jobs, CancellationToken cancellationToken)
    where T : TaskApiBaseResponse
  {
    while (cancellationToken.IsCancellationRequested == false)
    {
      try
      {
        // Check for subnodes before we even start polling,
        // to avoid pulling jobs and losing them / making http requests with no purpose
        subNodes.EnsureSubNodes();

        await foreach (var job in jobs.WithCancellation(cancellationToken))
        {
          logger.LogInformation("Task retrieved: ({Type}) {Id}", typeof(T).Name, job.Uuid);

          // Get up-to-date Sub Nodes list
          var subnodes = (await subNodes.List()).ToList();
          // Make sure there still are some; leave the loop if not
          if (!subnodes.Any()) break;

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
      catch (Exception e)
      {
        var delayTime = TimeSpan.FromSeconds(5);
        // Swallow exceptions and just log; the while loop will restart polling
        logger.LogError(e,
          "An error occurred handling '{TypeName}' tasks. Waiting {DelaySeconds}s before resuming polling.",
          typeof(T).Name,
          Math.Floor(delayTime.TotalSeconds));

        // TODO: maintain an exception limit that eventually DOES quit?

        // Delay before resuming the loop
        await Task
          .Delay(delayTime, cancellationToken)
          // Stop this Task from throwing when cancelled
          .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);
      }
    }
  }
}
