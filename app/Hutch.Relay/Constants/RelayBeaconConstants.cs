namespace Hutch.Relay.Constants;

public static class BeaconApiConstants
{
  /// <summary>
  /// Semantic Version of the GA4GH Beacon spec Relay implements
  /// </summary>
  public const string SpecVersion = "2.0.0";

  /// <summary>
  /// Beacon API Version Relay implements
  /// </summary>
  public const string ApiVersion = "v2.0";

  public const string RoutePrefix = "ga4gh/beacon/v2";

  public const string UpdateTermsHeader = "X-Relay-Beacon-UpdateTerms";

  public const string ForceUpdateTermsValue = "force";
}

public static class RelayBeaconTaskDetails
{
  public const string IdSuffix = "__RELAY_BEACON__";
  public const string Collection = "__RELAY_BEACON__";
  public const string Owner = "__RELAY_BEACON__";
}
