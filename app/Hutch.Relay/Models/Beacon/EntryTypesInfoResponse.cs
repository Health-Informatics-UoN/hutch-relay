namespace Hutch.Relay.Models.Beacon;

public class EntryTypesInfoResponse
{
  public required InfoMeta Meta { get; set; }

  public required EntryTypesInfoResponseBody Response { get; set; }
}

public class EntryTypesInfoResponseBody
{
  public required Dictionary<string, EntryTypeInfo> EntryTypes { get; set; }
}
