namespace Hutch.Relay.Constants;

public static class BeaconApiConstants
{
  public const string RoutePrefix = "ga4gh/beacon/v2";

  public const string UpdateTermsHeader = "X-Relay-Beacon-UpdateTerms";

  public const string ForceUpdateTermsValue = "force";
}

public static class RelayBeaconTaskDetails
{
  public const string IdPrefix = "__RELAY_BEACON__";
  public const string Collection = "__RELAY_BEACON__";
  public const string Owner = "__RELAY_BEACON__";
}
