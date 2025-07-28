using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Constants;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Services;

public class FilteringTermsService(
  ILogger<FilteringTermsService> logger,
  IOptions<RelayBeaconOptions> beaconOptions,
  ISubNodeService subNodes,
  IDownstreamTaskService downstreamTasks)
{
  public async Task RequestFilteringTerms()
  {
    if (!beaconOptions.Value.Enable)
    {
      logger.LogWarning("GA4GH Beacon Functionality is disabled; not requesting FilteringTerms.");
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
}
