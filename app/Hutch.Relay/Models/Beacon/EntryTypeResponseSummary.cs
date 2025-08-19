namespace Hutch.Relay.Models.Beacon;

public record EntryTypeResponseSummary
{
  public bool Exists { get; init; }
  
  public int? NumTotalResults { get; init; }
}
