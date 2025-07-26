using Hutch.Relay.Config.Beacon;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Services;

public class FilteringTermsService(
  ILogger<FilteringTermsService> logger,
  IOptions<RelayBeaconOptions> beaconOptions)
{
  public void RequestFilteringTerms()
  {
    if (!beaconOptions.Value.Enable)
    {
      logger.LogWarning("GA4GH Beacon Functionality is disabled; not requesting FilteringTerms.");
      return;
    }
  }
}
