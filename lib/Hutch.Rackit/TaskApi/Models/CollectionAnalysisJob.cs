using System.Text.Json.Serialization;

namespace Hutch.Rackit.TaskApi.Models;

public class CollectionAnalysisJob : TaskApiBaseResponse
{
  /// <summary>
  /// The code for the type of distribution query.
  /// Possible values:
  /// For "DISTRIBUTION" Analysis: "GENERIC", "DEMOGRAPHICS" or "ICD-MAIN"
  /// For "PHEWAS" Analysis: "" (empty)
  /// </summary>
  [JsonPropertyName("code")]
  public string Code { get; set; } = string.Empty;

  /// <summary>
  /// The type of analysis to be carried out.
  /// Possible values: "DISTRIBUTION", "PHEWAS"
  /// </summary>
  [JsonPropertyName("analysis")]
  public string Analysis { get; set; } = string.Empty;
}
