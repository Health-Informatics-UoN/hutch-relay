using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Constants;
using Hutch.Relay.Models.Beacon;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Services;

public class IndividualsQueryService(
  ILogger<IndividualsQueryService> logger,
  IOptions<RelayBeaconOptions> options,
  ISubNodeService subNodes,
  IDownstreamTaskService downstreamTasks)
{
  public IndividualsResponseSummary GetResultsSummary(int count)
  {
    IndividualsResponseSummary result = new()
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

  public IndividualsResponseSummary GetEmptySummary()
    => GetResultsSummary(0);

  public async Task<bool> TryEnqueueDownstream(List<string> queryTerms)
  {
    // Check Beacon Enabled (should never happen tbh)
    if (!options.Value.Enable)
    {
      logger.LogWarning(
        "GA4GH Beacon Functionality is disabled; Individuals query will not be queued."
      );
      return false;
    }

    // Short circuit if no terms
    if (queryTerms.Count < 1)
    {
      logger.LogWarning(
        "GA4GH Beacon Individuals Query with no Filters will not be queued."
      );
      return false;
    }

    // check for subnodes
    var subnodes = (await subNodes.List()).ToList();
    if (subnodes.Count == 0)
    {
      logger.LogError(
        "No subnodes are configured. The requested GA4GH Beacon Individuals Query will not be queued."
      );
      return false;
    }

    // Create the job and Enqueue it
    var availabilityJob = await CreateAvailabilityJob(queryTerms);
    await downstreamTasks.Enqueue(availabilityJob, subnodes);

    return true;
  }

  public static Task<AvailabilityJob> CreateAvailabilityJob(List<string> queryTerms)
  {
    if (queryTerms.Count < 1)
      throw new ArgumentException(
        "Expected at least one query term, but got none.", nameof(queryTerms));

    var rules = queryTerms
      .Select(term =>
        new Rule()
        {
          Operand = "=",
          Type = "TEXT",
          VariableName = "OMOP",
          Value = term[(term.IndexOf(':') + 1)..]
        }
      ).ToList();

    AvailabilityJob task = new()
    {
      Collection = RelayBeaconTaskDetails.Collection,
      Owner = RelayBeaconTaskDetails.Owner,
      Uuid = RelayBeaconTaskDetails.IdPrefix + Guid.NewGuid(),
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
