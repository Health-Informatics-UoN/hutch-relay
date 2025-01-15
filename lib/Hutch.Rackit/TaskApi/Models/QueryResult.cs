using System.Text.Json.Serialization;

namespace Hutch.Rackit.TaskApi.Models;

/// <summary>
/// This class represents the <c>queryResult</c> field in a <seealso cref="JobResult"/> object.
/// </summary>
public class QueryResult
{
  [JsonPropertyName("count")] public int Count { get; set; } = 0;

  [JsonPropertyName("datasetCount")] public int DatasetCount { get; set; } = 0;

  [JsonPropertyName("files")] public List<ResultFile> Files { get; set; } = new();
}
