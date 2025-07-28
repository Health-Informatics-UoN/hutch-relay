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

  // Simultaneously poll against all configured queues
  public async Task PollAllQueues(CancellationToken stoppingToken)
  {
    if (!options.Value.Enable)
    {
      logger.LogDebug("Upstream Task API functionality is disabled; Task polling will not be started.");
      return; // nothing to do!
    }

    // Test Queue Backend availability
    if (!await queues.IsReady())
      throw new InvalidOperationException(
        "The RelayTask Queue Backend is not ready; please check the logs and your configuration.");

    var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

    if (string.IsNullOrWhiteSpace(options.Value.QueueTypes))
    {
      await PollUnifiedJobQueue(cts.Token);
    }
    else
    {
      // Setup polling threads for each job type
      // Passing the same CancellationTokenSource allows us to cancel all the polling tasks when one of them fails
      List<Task> pollers = [];

      var types = options.Value.QueueTypes.Split(",");

      if (types.Contains("a") || types.Contains("*"))
        pollers.Add(PollJobQueue<AvailabilityJob>(cts));

      if (types.Contains("b") || types.Contains("*"))
        pollers.Add(PollJobQueue<CollectionAnalysisJob>(cts));

      // TODO: Type C, D

      // start parallel polling threads
      await Task.WhenAll(pollers);
    }

  }

  private static T ConvertTaskType<T>(TaskApiBaseResponse job) where T : TaskApiBaseResponse, new()
  {
    return (T)Convert.ChangeType(job, typeof(T));
  }

  private async Task PollUnifiedJobQueue(CancellationToken cancellationToken)
  {
    var jobs = upstreamTasks.PollUnifiedJobQueue(options.Value, cancellationToken);

    while (cancellationToken.IsCancellationRequested == false)
    {
      try
      {
        // Check for subnodes before we even start polling,
        // to avoid pulling jobs and losing them / making http requests with no purpose
        subNodes.EnsureSubNodes(); // Though this uses the parent's scoped db context and we poll on different threads, it is an untracked read operation

        await foreach (var (type, job) in jobs.WithCancellation(cancellationToken))
        {
          using var scope = serviceScopeFactory.CreateScope(); // create a new scope for each job (similar to aspnet per-request scope)
          var handler = scope.ServiceProvider.GetRequiredService<ScopedTaskHandler>(); // this will therefore get a scoped DbContext

          switch (type.Name)
          {
            case nameof(AvailabilityJob):
              await handler.HandleTask(ConvertTaskType<AvailabilityJob>(job));
              break;
            case nameof(CollectionAnalysisJob):
              await handler.HandleTask(ConvertTaskType<CollectionAnalysisJob>(job));
              break;
            default: throw new InvalidOperationException($"Invalid task type received from Upstream unified queue: {type.Name}");
          }
        }
      }
      catch (Exception e)
      {
        // By default, exceptions should terminate; preferable in e.g. container environments
        // unless configured otherwise, or for gracefully handled specific cases

        // Always log
        logger.LogError(e, "An error occurred handling tasks from Upstream unified queue.");

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
            .Delay(delayTime, cancellationToken)
            // Stop this Task from throwing when cancelled
            .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);
        }
        else
        {
          throw;
        }
      }
    }
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
  IDownstreamTaskService downstreamTasks,
  ISubNodeService subNodes)
{
  public async Task HandleTask<T>(T job)
    where T : TaskApiBaseResponse
  {
    logger.LogInformation("Task retrieved: ({Type}) {Id}", typeof(T).Name, job.Uuid);

    // Get up-to-date Sub Nodes list
    var subnodes = (await subNodes.List()).ToList();
    // Make sure there still are some; leave the loop if not
    if (subnodes.Count == 0) return;

    try
    {
      await downstreamTasks.Enqueue(job, subnodes);
    }
    catch (ArgumentOutOfRangeException e)
    {
      if (e.Message.Contains("ICD-MAIN"))
      {
        logger.LogInformation("Skipping unsupported ICD-MAIN Distribution task: {Id}", job.Uuid);
        return;
      }
      throw;
    }
  }
}
