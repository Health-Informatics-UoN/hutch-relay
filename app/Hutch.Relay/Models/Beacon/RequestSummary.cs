using System.Text.Json.Serialization;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Constants;

namespace Hutch.Relay.Models.Beacon;

public class RequestSummary
{
  public string ApiVersion { get; set; } = BeaconApiConstants.ApiVersion;

  [JsonPropertyName("requestedSchemas")]
  public List<ReturnedSchema> Schemas { get; set; } = [];

  public List<string> Filters { get; set; } = [];

  [JsonPropertyName("requestParameters")] // TODO: implement for POST
  public Dictionary<string, string>? Parameters { get; } // TODO: $schema property?

  public string IncludeResultsetResponses { get; set; } = nameof(ResultsetResponses.HIT); // TODO: We don't support miss today (and i expect never will? but may need to revisit when POST endpoints are added)

  /// <summary>
  /// Pagination to apply or that has been applied on the results.
  /// </summary>
  public Pagination Pagination { get; set; } = new();

  [JsonPropertyName("requestedGranularity")]
  public required string Granularity { get; set; } // TODO: Implement more accurately for POST

  public bool TestMode { get; set; } = false; // No way to set `true` until we support POST
}

// Because Relay never returns record granularity, pagination is never relevant to entrytype responses
// This model is accurate to the standard, but since even RequestSummary can return the applied pagination, Relay need never use it.
public class Pagination
{
  public int? Skip { get; set; }
  public int? Limit { get; set; }

  public string? CurrentPage { get; set; }

  public string? NextPage { get; set; }

  public string? PreviousPage { get; set; }
}
