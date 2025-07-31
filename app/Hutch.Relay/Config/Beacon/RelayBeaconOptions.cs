namespace Hutch.Relay.Config.Beacon;

public class RelayBeaconOptions : BaseBeaconOptions
{
  /// <summary>
  /// Enable or disable the Beacon API.
  /// </summary>
  public bool Enable { get; set; } = false;

  /// <summary>
  /// If Beacon is enabled, should Relay immediately request
  /// filtering terms from configured subnodes on startup?
  /// </summary>
  public bool RequestFilteringTermsOnStartup { get; set; } = true;
}
