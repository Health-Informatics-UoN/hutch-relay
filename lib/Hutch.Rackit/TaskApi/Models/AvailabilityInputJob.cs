using System.Text.Json.Serialization;

namespace Hutch.Rackit.TaskApi.Models;

public class AvailabilityInputJob : AvailabilityJob
{
  /// <summary>
  /// The list of collection IDs to run the query against.
  /// 
  /// Collection is a list when creating a new task.
  /// </summary>
  [JsonPropertyName("collection")]
  public new List<string> Collection { get; set; } = new List<string>();
}
