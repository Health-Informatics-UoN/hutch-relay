using Hutch.Relay.Config;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Constants;
using Hutch.Relay.Models.Beacon;
using Hutch.Relay.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Mvc;

namespace Hutch.Relay.Controllers.Beacon;

[ApiExplorerSettings(GroupName = ApiExplorerGroups.BeaconName)]
[FeatureGate(Features.Beacon)]
[ApiController]
[Route($"{BeaconApiConstants.RoutePrefix}/filtering_terms")]
public class FilteringTermsController(
  IOptions<RelayBeaconOptions> options,
  IFilteringTermsService filteringTerms) : ControllerBase
{
  private readonly RelayBeaconOptions _options = options.Value;

  [HttpGet]
  public async Task<FilteringTermsResponse> List(int skip = 0, int limit = 10)
  {
    // Check for custom header to queue update request
    var hasDownstreamUpdateHeader = Request.Headers.TryGetValue(
      BeaconApiConstants.UpdateTermsHeader,
      out var downstreamUpdateHeader);

    var mustForceDownstreamUpdate = hasDownstreamUpdateHeader &&
      string.Equals(
        downstreamUpdateHeader, BeaconApiConstants.ForceUpdateTermsValue,
        StringComparison.InvariantCultureIgnoreCase);

    // Fetch cached terms
    var terms = await filteringTerms.List(skip, limit);

    // Request terms update if applicable
    if (hasDownstreamUpdateHeader || terms.Count == 0)
      await filteringTerms.RequestUpdatedTerms(mustForceDownstreamUpdate);

    return new()
    {
      Meta = new()
      {
        BeaconId = _options.Info.Id,
        ReturnedSchemas = {
          new() {
            EntityType = "filteringTerm",
            Schema = "https://raw.githubusercontent.com/ga4gh-beacon/beacon-framework-v2/main/definitions/FilteringTerm"
          }
        }
      },
      Response = new() {
        FilteringTerms = terms
      }
    };

  }
}
