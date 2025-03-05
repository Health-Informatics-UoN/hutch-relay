using System.Text.Json.Serialization;

namespace Hutch.Rackit.TaskApi.Models;

public class TaskApiBaseInput
{
  /// <summary>
  /// The application name. Only "AVAILABILITY_QUERY" is supported at this time.
  /// </summary>
  [JsonPropertyName("application")] public string Application { get; set; } = "AVAILABILITY_QUERY";

  /// <summary>
  /// The input for the task.
  /// </summary>
  [JsonPropertyName("input")] public AvailabilityInputJob Input { get; set; } = new AvailabilityInputJob();
}
