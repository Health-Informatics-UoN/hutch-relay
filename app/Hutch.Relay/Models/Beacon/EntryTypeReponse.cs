namespace Hutch.Relay.Models.Beacon;

public class EntryTypeResponse
{
  //NOTE: "Info" is an optional placeholder, so Relay omits it currently:
  // https://b2ri-documentation.readthedocs.io/en/latest/endpoints/individuals.html#tag/GET-Endpoints/operation/getIndividuals
  
  public required Meta Meta { get; set; }
  
  public required EntryTypeResponseSummary ResponseSummary { get; set; }

  public List<BeaconHandover>? BeaconHandovers { get; set; }
}

// TODO: understand what Handovers are for? Will Relay ever use them?
public class BeaconHandover
{
  public HandoverType HandoverType { get; set; } = new();

  public string Url { get; set; } = string.Empty;
}

public class HandoverType
{
  public string Id { get; set; } = string.Empty;

  public string Label { get; set; } = string.Empty;
}
