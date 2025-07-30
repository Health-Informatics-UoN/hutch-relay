using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Constants;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Services;

public class FilteringTermsService(
  ILogger<FilteringTermsService> logger,
  IOptions<RelayBeaconOptions> beaconOptions,
  ISubNodeService subNodes,
  IDownstreamTaskService downstreamTasks)
{
  public async Task RequestUpdatedTerms()
  {
    if (!beaconOptions.Value.Enable)
    {
      logger.LogWarning("GA4GH Beacon Functionality is disabled; not requesting updated Filtering Terms.");
      return;
    }

    // Get up-to-date Sub Nodes list
    var subnodes = (await subNodes.List()).ToList();

    var task = new CollectionAnalysisJob()
    {
      Uuid = RelayBeaconTaskDetails.IdPrefix + Guid.NewGuid().ToString(),
      Analysis = AnalysisType.Distribution,
      Code = DistributionCode.Generic,
      Collection = RelayBeaconTaskDetails.Collection,
      Owner = RelayBeaconTaskDetails.Owner
    };

    await downstreamTasks.Enqueue(task, subnodes);
  }

  // public async Task CacheUpdatedTerms(JobResult finalResult)
  // {

  // }

  internal static List<FilteringTerm> Map(List<GenericDistributionRecord> records)
  {
    return [.. records.Select(Map)];
  }

  internal static FilteringTerm Map(GenericDistributionRecord record)
  {
    return new()
    {
      Term = record.Code,
      SourceCategory = record.Category,
      VarCat = CodeCategory.VarCatMap.GetValueOrDefault(record.Category),

      // Prefer OMOP Description if provided
      Description = string.IsNullOrWhiteSpace(record.OmopDescription)
        ? record.Description
        : record.OmopDescription
    };
  }
}
