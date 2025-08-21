using Hutch.Relay.Config.Helpers;

namespace Hutch.Relay.Config.Beacon;

public class RelayBeaconOptions : BaseBeaconOptions, IFeatureOptionsModel
{
  /// <summary>
  /// Enable or disable the Beacon API.
  /// </summary>
  public bool Enable { get; set; } = false;

  /// <summary>
  /// If Beacon is enabled, should Relay immediately request
  /// filtering terms from configured subnodes on startup?
  /// </summary>
  public StartupFilteringTermsBehaviour RequestFilteringTermsOnStartup { get; set; }
}

/// <summary>
/// Valid options for <see cref="RelayBeaconOptions.RequestFilteringTermsOnStartup"/> 
/// </summary>
public enum StartupFilteringTermsBehaviour
{
  /// <summary>
  /// Default. Do not request Beacon Filtering Terms when Relay starts.
  /// </summary>
  Never,

  /// <summary>
  /// Request Beacon Filtering Terms when Relay starts only if there are none cached.
  /// If there are already in-progress tasks for Filtering Terms, new ones will not be queued
  /// </summary>
  IfEmpty,

  /// <summary>
  /// Force Request Beacon Filtering Terms when Relay starts only if there are none cached.
  /// New Filtering Terms tasks will be queued even if there are some in-progress.
  /// </summary>
  ForceIfEmpty,

  /// <summary>
  /// Request Beacon Filtering Terms everytime Relay starts.
  /// If there are already in-progress tasks for Filtering Terms, new ones will not be queued
  /// </summary>
  Always,

  /// <summary>
  /// Force Request Beacon Filtering Terms everytime Relay starts.
  /// New Filtering Terms tasks will be queued even if there are some in-progress.
  /// </summary>
  ForceAlways
}
