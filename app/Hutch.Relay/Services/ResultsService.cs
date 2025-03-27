using System.Net;
using Hutch.Rackit;
using Hutch.Rackit.TaskApi.Contracts;
using Hutch.Rackit.TaskApi.Models;
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
  [FromKeyedServices(nameof(AvailabilityAggregator))]
  IQueryResultAggregator availabilityAggregator,
  [FromKeyedServices(nameof(GenericDistributionAggregator))]
  IQueryResultAggregator codeDistributionAggregator
)
{
  private readonly ApiClientOptions options = options.Value;

  /// <summary>
  /// Submit a <see cref="JobResult"/> payload Upstream
  /// </summary>
  /// <param name="relayTask"><see cref="RelayTaskModel"/> describing the RelayTask to submit results for.</param>
  /// <param name="jobResult">The <see cref="JobResult"/> payload to submit.</param>
  public async Task SubmitResults(RelayTaskModel relayTask, JobResult jobResult)
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
        else throw;
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
    try
    {
      var finalResult = await PrepareFinalJobResult(task);

      // Only submit results if Prepare succeeded
      await SubmitResults(task, finalResult);
    }
    catch (ArgumentOutOfRangeException e)
    {
      // Catch and log, but otherwise no particular handling for Unsupported Task Types
      logger.LogError(e.Message);
    }
    finally
    {
      // We should try and close out the task regardless of whether an exception occurred
      await relayTaskService.SetComplete(task.Id);
    }
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
      TaskTypes.TaskApi_Availability => availabilityAggregator,
      TaskTypes.TaskApi_CodeDistribution => codeDistributionAggregator,
      _ => throw new ArgumentOutOfRangeException(
        $"Relay tried to handle a Task Type it doesn't support Results Aggregation for: {relayTask.Type}")
    };

    // Set the correct upstream values in the JobResult, and set the aggregate, obfuscated QueryResult data.
    return new()
    {
      Uuid = relayTask.Id,
      CollectionId = relayTask.Collection,
      Results = aggregator.Process(subTasks)
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
      var expiryThreshold = TimeSpan.FromMinutes(4); // TODO: should this be configurable?

      var timeInterval = DateTimeOffset.UtcNow.Subtract(task.CreatedAt);
      logger.LogInformation("Incomplete Task:{Task} has been running for {TimeInterval}...", task.Id,
        timeInterval.ToString());
      if (timeInterval > expiryThreshold)
      {
        logger.LogInformation("Task:{Task} has reached the expiry threshold of {ExpiryThreshold}. Completing...",
          task.Id, expiryThreshold.ToString());
        await CompleteRelayTask(task);
      }
    }
  }
}
