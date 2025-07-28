using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config.Beacon;
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
      Analysis = AnalysisType.Distribution,
      Code = DistributionCode.Generic,
      Collection = "", // TODO: Beacon Collection constant
      Owner = "" // TODO: Should this be an internal user for subnode purposes? A constant Beacon user?
    };

    await downstreamTasks.Enqueue(task, subnodes);
  }
}
