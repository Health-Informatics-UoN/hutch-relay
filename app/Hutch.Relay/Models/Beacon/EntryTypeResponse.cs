namespace Hutch.Relay.Models.Beacon;

public class EntryTypeResponse
{
  public Dictionary<string, object>? Info { get; } // "Info" is an optional placeholder, so Relay omits it currently

  public required EntryTypeMeta Meta { get; set; }

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
