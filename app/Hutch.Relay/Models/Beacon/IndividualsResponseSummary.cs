namespace Hutch.Relay.Models.Beacon;

public record IndividualsResponseSummary
{
  public bool Exists { get; init; }
  
  public int? NumTotalResults { get; init; }
}
