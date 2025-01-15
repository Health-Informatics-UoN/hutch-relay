using System.Text.Json.Serialization;

namespace Hutch.Rackit.TaskApi.Models;

public abstract class TaskApiBaseResponse
{
  internal TaskApiBaseResponse() { } // Prevents the base class being derived from outside the assembly

  /// <summary>
  /// The unique Job ID assigned by the API
  /// </summary>
  [JsonPropertyName("uuid")]
  public string Uuid { get; set; } = string.Empty;

  /// <summary>
  /// The collection ID to run the query against.
  /// </summary>
  [JsonPropertyName("collection")]
  public string Collection { get; set; } = string.Empty;

  /// <summary>
  /// The API user that made the request.
  /// </summary>
  [JsonPropertyName("owner")]
  public string Owner { get; set; } = string.Empty;
}
