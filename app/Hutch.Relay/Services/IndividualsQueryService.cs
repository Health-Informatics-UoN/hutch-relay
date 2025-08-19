using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Constants;
using Hutch.Relay.Extensions;
using Hutch.Relay.Models.Beacon;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Services;

public class IndividualsQueryService(
  ILogger<IndividualsQueryService> logger,
  IOptions<RelayBeaconOptions> options,
  ISubNodeService subNodes,
  IDownstreamTaskService downstreamTasks,
  IBeaconResultsQueue resultsQueue)
{
  public EntryTypeResponseSummary GetResultsSummary(int count)
  {
    EntryTypeResponseSummary result = new()
    {
      Exists = count > 0
    };

    // TODO: how is "non-default" granularity specified?
    // TODO: meta etc.
    return options.Value.SecurityAttributes.DefaultGranularity switch
    {
      Granularity.boolean => result,
      _ => result with { NumTotalResults = count }
    };
  }

  public EntryTypeResponseSummary GetEmptySummary()
    => GetResultsSummary(0);

  public async Task<string?> EnqueueDownstream(List<string> queryTerms)
  {
    // Check Beacon Enabled (should never happen tbh)
    if (!options.Value.Enable)
    {
      logger.LogWarning(
        "GA4GH Beacon Functionality is disabled; Individuals query will not be queued."
      );
      return null;
    }

    // Short circuit if no terms
    if (queryTerms.Count < 1)
    {
      logger.LogWarning(
        "GA4GH Beacon Individuals Query with no Filters will not be queued."
      );
      return null;
    }

    // check for subnodes
    var subnodes = (await subNodes.List()).ToList();
    if (subnodes.Count == 0)
    {
      logger.LogError(
        "No subnodes are configured. The requested GA4GH Beacon Individuals Query will not be queued."
      );
      return null;
    }

    // Create the job and Enqueue it
    var queueName = await resultsQueue.CreateResultsQueue();
    
    var availabilityJob = await CreateAvailabilityJob(queryTerms, queueName);
    
    await downstreamTasks.Enqueue(availabilityJob, subnodes);

    return queueName;
  }

  /// <summary>
  /// Await results from a job-specific queue, which should have been created by <see cref="EnqueueDownstream"/>
  /// </summary>
  /// <param name="queueName">The name of the queue to check for results</param>
  /// <returns>The query results as soon as they are available</returns>
  /// <exception cref="ArgumentException">When queue name is an empty value</exception>
  public async Task<EntryTypeResponseSummary> AwaitResults(string queueName)
  {
    if (string.IsNullOrWhiteSpace(queueName))
      throw new ArgumentException(
        "Invalid queue name provided; unable to await results from unknown queue.", 
        queueName);
    
    var count = await resultsQueue.AwaitResults(queueName);

    return GetResultsSummary(count);
  }

  public static Task<AvailabilityJob> CreateAvailabilityJob(List<string> queryTerms, string queueName)
  {
    if (queryTerms.Count < 1)
      throw new ArgumentException(
        "Expected at least one query term, but got none.", nameof(queryTerms));
    
    if (string.IsNullOrWhiteSpace(queueName))
      throw new ArgumentException(
        "Beacon Individuals queries must include queue names in their AvailabilityJob ID.", 
        queueName);

    var rules = queryTerms
      .Select(term =>
        new Rule()
        {
          Operand = "=",
          Type = "TEXT",
          VariableName = "OMOP",
          Value = term.ExtractAfterSubstring(":")
        }
      ).ToList();

    AvailabilityJob task = new()
    {
      Collection = RelayBeaconTaskDetails.Collection,
      Owner = RelayBeaconTaskDetails.Owner,
      Uuid = Guid.NewGuid() + RelayBeaconTaskDetails.IdSuffix + queueName,
      Cohort = new()
      {
        Combinator = "OR",
        Groups =
        [
          new()
          {
            Combinator = "AND",
            Rules = rules
          }
        ]
      }
    };

    return Task.FromResult(task);
  }
}
