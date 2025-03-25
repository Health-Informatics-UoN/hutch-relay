using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Hutch.Rackit;
using Hutch.Rackit.TaskApi.Contracts;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config;
using Hutch.Relay.Constants;
using Hutch.Relay.Models;
using Hutch.Relay.Services.Contracts;
using Hutch.Relay.Services.JobResultAggregators;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Services;

public class ResultsService(
  ILogger<ResultsService> logger,
  IOptions<ApiClientOptions> options,
  ITaskApiClient upstreamTasks,
  IRelayTaskService relayTaskService,
  IOptions<ObfuscationOptions> obfuscationOptions)
{
  private readonly ApiClientOptions options = options.Value;
  private readonly ObfuscationOptions obfuscationOptions = obfuscationOptions.Value;

  /// <summary>
  /// Submit a <see cref="JobResult"/> payload Upstream
  /// </summary>
  /// <param name="relayTask"><see cref="RelayTaskModel"/> describing the RelayTask to submit results for.</param>
  /// <param name="jobResult">The <see cref="JobResult"/> payload to submit.</param>
  public async Task SubmitResults(RelayTaskModel relayTask, JobResult jobResult
  )
  {
    var retryCount = 0;
    const int delayInSeconds = 5;
    const int maxRetryCount = 5;
    while (retryCount < maxRetryCount)
    {
      logger.LogInformation("Submitting Results for {Task}..", relayTask.Id);
      try
      {
        // Submit results upstream
        await upstreamTasks.SubmitResultAsync(relayTask.Id, jobResult, options);
        logger.LogInformation("Successfully submitted results for {RelayTaskId}", relayTask.Id);
        break;
      }
      catch (RackitApiClientException exception)
      {
        if (exception.UpstreamApiResponse is { StatusCode: HttpStatusCode.InternalServerError })
        {
          retryCount++;
          logger.LogError(
            "Task submission failed with 500 Internal Server Error. Retrying in {delayInSeconds} seconds... ({retryCount}/{maxRetries})",
            delayInSeconds,
            retryCount, maxRetryCount);

          await Task.Delay(delayInSeconds * 1000);
        }
      }
    }
  }

  /// <summary>
  ///   <list type="number">
  ///     <listheader>
  ///       Complete a <see cref="Data.Entities.RelayTask"/> by:
  ///     </listheader>
  ///     <item>
  ///       Aggregating all currently received results from its subtasks
  ///     </item>
  ///     <item>
  ///       Applying obfuscation routines to the aggregate values
  ///     </item>
  ///     <item>
  ///       Submitting the final results Upstream
  ///     </item>
  ///     <item>
  ///       Marking the <see cref="Data.Entities.RelayTask"/> as complete in the Relay datastore.
  ///     </item>
  ///   </list>
  /// 1. 
  /// </summary>
  /// <param name="task"><see cref="RelayTaskModel"/> for the task to Complete.</param>
  public async Task CompleteRelayTask(RelayTaskModel task)
  {
    var finalResult = await PrepareFinalJobResult(task);
    
    await SubmitResults(task, finalResult);
    
    await relayTaskService.SetComplete(task.Id);
  }

  /// <summary>
  /// Aggregates Sub Task results and obfuscates aggregate values. The details of this behaviour are specific to a given Task Type,
  /// by means of an <see cref="IQueryResultAggregator"/> implementation.
  ///
  /// Adds the final aggregate results to a <see cref="JobResult"/> payload for the RelayTask, suitable to submit upstream.
  /// </summary>
  /// <param name="relayTask"><see cref="RelayTaskModel"/> providing details of the RelayTask to prepare a <see cref="JobResult"/> for.</param>
  /// <returns>The <see cref="JobResult"/> containing aggregated and obfuscated data from all Sub Tasks.</returns>
  /// <exception cref="ArgumentOutOfRangeException">The Task Type of this <see cref="RelayTaskModel"/> isn't supported by Relay. Who knows how it got this far.</exception>
  public async Task<JobResult> PrepareFinalJobResult(RelayTaskModel relayTask)
  {
    // Get all SubTasks for this RelayTask
    var subTasks = (await relayTaskService.ListSubTasks(relayTask.Id, incompleteOnly: false)).ToList();

    // Select the appropriate Results Aggregator
    IQueryResultAggregator aggregator = relayTask.Type switch
    {
      TaskTypes.TaskApi_Availability => new AvailabilityAggregator(),
      _ => throw new ArgumentOutOfRangeException(
        $"Relay tried to handle a Task Type it doesn't support Results Aggregation for: {relayTask.Type}")
    };

    // Set the correct upstream values in the JobResult, and set the aggregate, obfuscated QueryResult data.
    return new()
    {
      Uuid = relayTask.Id,
      CollectionId = relayTask.Collection,
      Results = aggregator.Aggregate(subTasks, obfuscationOptions)
    };
  }

  /// <summary>
  /// Check for incomplete RelayTasks past the timeout threshold and attempt to Complete them
  /// </summary>
  public async Task HandleResultsToExpire()
  {
    var incompleteTasks = await relayTaskService.ListIncomplete();
    foreach (var task in incompleteTasks)
    {
      logger.LogInformation("Task:{Task} is about to expire.", task.Id);
      var timeInterval = DateTimeOffset.UtcNow.Subtract(task.CreatedAt);
      if (timeInterval > TimeSpan.FromMinutes(4.5)) // TODO: should this be configurable?
      {
        await CompleteRelayTask(task);
      }
    }
  }
}
