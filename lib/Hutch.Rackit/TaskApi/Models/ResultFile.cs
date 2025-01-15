using System.Text;
using System.Text.Json.Serialization;

namespace Hutch.Rackit.TaskApi.Models;

/// <summary>
/// This class represents an individual file in the <c>Files<c> property of a <seealso cref="QueryResult"/>.
/// </summary>
public class ResultFile
{
  /// <summary>
  /// Base64 encoded file contents
  /// </summary>
  [JsonPropertyName("file_data")] public string FileData { get; set; } = string.Empty;

  /// <summary>
  /// Can be empty.
  /// </summary>
  [JsonPropertyName("file_name")] public string FileName { get; set; } = string.Empty;

  /// <summary>
  /// Useful to describe file contents, e.g. the type of analysis the results are for.
  /// </summary>
  [JsonPropertyName("file_description")] public string? FileDescription { get; set; } = null;

  /// <summary>
  /// Unknown usage. Can be empty.
  /// </summary>
  [JsonPropertyName("file_reference")] public string FileReference { get; set; } = string.Empty;

  /// <summary>
  /// Always true in practice?
  /// </summary>
  [JsonPropertyName("file_sensitive")] public bool FileSensitive { get; set; } = true;

  /// <summary>
  /// Size in bytes of the data
  /// </summary>
  [JsonPropertyName("file_size")] public double FileSize { get; set; } = 0.0;

  /// <summary>
  /// Always `BCOS` today. Doesn't record the original file type of the data.
  /// </summary>
  [JsonPropertyName("file_type")] public string FileType { get; set; } = "BCOS";
}

public static class ResultFileExtensions
{
  /// <summary>
  /// Add plain text data to a ResultFile. This method encodes and sets the <see cref="ResultFile.FileData"/> and <see cref="ResultFile.FileSize"/>
  /// properties based on the data provided.
  /// </summary>
  /// <param name="resultFile">the <see cref="ResultFile"/> to set properties on.</param>
  /// <param name="data">The data to encode and set</param>
  /// <returns>The modified <see cref="ResultFile"/>.</returns>
  public static ResultFile WithData(this ResultFile resultFile, string data)
  {
    var bytes = Encoding.UTF8.GetBytes(data); // need the interim bytes to get file size
    return resultFile.WithData(bytes);
  }

  /// <summary>
  /// Add data to a ResultFile. This method encodes and sets the <see cref="ResultFile.FileData"/> and <see cref="ResultFile.FileSize"/>
  /// properties based on the data provided.
  /// </summary>
  /// <param name="resultFile">the <see cref="ResultFile"/> to set properties on.</param>
  /// <param name="data">The data to encode and set</param>
  /// <returns>The modified <see cref="ResultFile"/>.</returns>
  public static ResultFile WithData(this ResultFile resultFile, byte[] data)
  {
    resultFile.FileData = Convert.ToBase64String(data);
    resultFile.FileSize = data.Length;
    return resultFile;
  }
}
