using Hutch.Relay.Config.Beacon;

namespace Hutch.Relay.Models.Beacon;

public class EntryTypeMeta : InfoMeta
{
  public string ReturnedGranularity { get; set; } = nameof(Granularity.boolean);

  public required RequestSummary ReceivedRequestSummary { get; set; }

  public bool TestMode { get; }
}
