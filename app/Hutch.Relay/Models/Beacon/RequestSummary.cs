using Hutch.Relay.Config.Beacon;

namespace Hutch.Relay.Models.Beacon;

public class RequestSummary
{
  public string? ApiVersion { get; set; }

  public string? Schemas { get; set; }

  public List<string> Filters { get; set; } = [];

  public string? Parameters { get; set; }

  //public string? IncludeResultSetResponses { get; set; } = ResultsetResponses.Hit; // TODO: We don't support miss today (and i expect never will? but may need to revisit when POST endpoints are added)

  public Pagination Pagination { get; set; } = new();

  public required Granularity Granularity { get; set; }

  public bool TestMode { get; set; } = false; // No way to set `true` until we support POST
}

public class Pagination
{
  public int Skip { get; set; }
  public int Limit { get; set; }
}
