using Hutch.Relay.Config;
using Hutch.Relay.Constants;
using Hutch.Relay.Models.Beacon;
using Hutch.Relay.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace Hutch.Relay.Controllers.Beacon;

[FeatureGate(Features.Beacon)]
[ApiController]
[Route($"{BeaconApiConstants.RoutePrefix}/filtering_terms")]
public class FilteringTermsController(IFilteringTermsService filteringTerms) : ControllerBase
{
  [HttpGet]
  public async Task<List<FilteringTerm>> List(int skip = 0, int limit = 10)
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
    var terms = await filteringTerms.List(skip, limit); // TODO: SKIP a PAGE not a RECORD. Unit tests SKIP!

    // Request terms update if applicable
    if (hasDownstreamUpdateHeader || terms.Count == 0)
      await filteringTerms.RequestUpdatedTerms(mustForceDownstreamUpdate);
    
    return terms;
  }
}
