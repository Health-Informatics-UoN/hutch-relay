using System.Net;
using System.Text.Json;
using Hutch.Rackit;
using Hutch.Rackit.TaskApi.Contracts;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Models;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Services;

public class ResultsService(
  ILogger<ResultsService> logger,
  IOptions<ApiClientOptions> options,
  ITaskApiClient upstreamTasks,
  IRelayTaskService relayTaskService,
  IObfuscationService obfuscationService)
{
  private ApiClientOptions options = options.Value;

  public async Task SubmitResults(RelayTaskModel relayTask, JobResult jobResult
  )
  {
    int retryCount = 0;
    int delayInSeconds = 5;
    int maxRetryCount = 5;
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

  public async Task<JobResult> AggregateResults(string relayTaskId)
  {
    // Get all SubTasks for the Task
    var subTasks = await relayTaskService.ListSubTasks(relayTaskId, incompleteOnly: false);
    int aggregateCount = 0;
    foreach (var subTask in subTasks)
    {
      if (subTask.Result != null)
      {
        var result = JsonSerializer.Deserialize<JobResult>(subTask.Result) ?? throw new NullReferenceException();
        aggregateCount += result.Results.Count;
      }
    }

    return new JobResult()
    {
      Uuid = relayTaskId,
      CollectionId = options.CollectionId ??
                     throw new ArgumentException(nameof(options.CollectionId)),
      Results = new QueryResult()
      {
        Count = aggregateCount,
      }
    };
  }

  public async Task HandleResultsToExpire()
  {
    var incompleteTasks = await relayTaskService.ListIncomplete();
    foreach (var task in incompleteTasks)
    {
      logger.LogInformation("Task:{Task} is about to expire.", task.Id);
      var timeInterval = DateTimeOffset.UtcNow.Subtract(task.CreatedAt);
      if (timeInterval > TimeSpan.FromMinutes(4.5))
      {
        // Aggregate SubTask count
        var finalResult = await AggregateResults(task.Id);
        // Obfuscate the result
        finalResult.Results.Count = obfuscationService.Obfuscate(finalResult.Results.Count);
        // Post to TaskApi 
        await SubmitResults(task, finalResult);
        //Set task as complete
        await relayTaskService.SetComplete(task.Id);
      }
    }
  }
}
