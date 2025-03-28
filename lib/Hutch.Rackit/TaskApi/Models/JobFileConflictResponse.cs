using System.Text.Json.Serialization;

namespace Hutch.Rackit.TaskApi.Models;

public class JobFileConflictResponse
{
  /// <summary>
  /// Response Status
  /// </summary>
  [JsonPropertyName("status")]
  public string Status { get; set; } = ResultResponseStatus.Conflict;
  
  /// <summary>
  /// Timestamp of response generation. The output date format is as current Task API implementations generate
  /// </summary>
  [JsonPropertyName("timestamp")]
  public string Timestamp { get; set; } = DateTimeOffset.UtcNow.ToString("MM-dd-yyyy HH:mm:ss");

  /// <summary>
  /// The message
  /// </summary>
  [JsonPropertyName("message")]
  public string Message { get; set; } = "Job file already uploaded";
  
  /// <summary>
  /// Debug message which may match <see cref="Message"/> or may contain additional details.
  /// </summary>
  [JsonPropertyName("debugMessage")]
  public string DebugMessage { get; set; } = "Job file already uploaded";
}
