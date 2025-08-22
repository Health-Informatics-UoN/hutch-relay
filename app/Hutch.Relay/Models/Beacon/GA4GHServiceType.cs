using Hutch.Relay.Constants;

namespace Hutch.Relay.Models.Beacon;

public class GA4GHServiceType
{
  public string Group { get; set; } = "org.ga4gh";
  public string Artifact { get; set; } = "beacon";
  public string Version { get; set; } = BeaconApiConstants.SpecVersion; // TODO: confirm what it takes to support latest https://docs.genomebeacons.org/changes-todo/#changes https://github.com/ga4gh-beacon/beacon-v2/releases
}
