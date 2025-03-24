using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using CsvHelper;
using CsvHelper.Configuration;

namespace Hutch.Rackit.TaskApi.Models;

/// <summary>
/// This class represents an individual file in the <c>Files<c> property of a <seealso cref="QueryResult"/>.
/// </summary>
public class ResultFile
{
  /// <summary>
  /// Base64 encoded file contents
  /// </summary>
  [JsonPropertyName("file_data")]
  public string FileData { get; set; } = string.Empty;

  /// <summary>
  /// Must not be empty. Should match the results type provided within.
  /// Valid supported RACKit values: `code.distribution`, `demographics.distribution`
  /// </summary>
  [JsonPropertyName("file_name")]
  public string FileName { get; set; } = string.Empty;

  /// <summary>
  /// Useful to describe file contents, e.g. the type of analysis the results are for.
  /// </summary>
  [JsonPropertyName("file_description")]
  public string? FileDescription { get; set; } = null;

  /// <summary>
  /// Unknown usage. Can be empty.
  /// </summary>
  [JsonPropertyName("file_reference")]
  public string FileReference { get; set; } = string.Empty;

  /// <summary>
  /// Always true in practice?
  /// </summary>
  [JsonPropertyName("file_sensitive")]
  public bool FileSensitive { get; set; } = true;

  /// <summary>
  /// Size in bytes of the data
  /// </summary>
  [JsonPropertyName("file_size")]
  public double FileSize { get; set; } = 0.0;

  /// <summary>
  /// Always `BCOS` today. Doesn't record the original file type of the data.
  /// </summary>
  [JsonPropertyName("file_type")]
  public string FileType { get; set; } = "BCOS";
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

  /// <summary>
  /// Add <see cref="DistributionRecord"/> Results data to a ResultFile. This method encodes and sets the <see cref="ResultFile.FileData"/> and <see cref="ResultFile.FileSize"/>
  /// properties based on the data provided.
  /// </summary>
  /// <param name="resultFile">the <see cref="ResultFile"/> to set properties on.</param>
  /// <param name="distributionResults">The data to encode and set</param>
  /// <returns>The modified <see cref="ResultFile"/>.</returns>
  public static ResultFile WithDistributionResults(
    this ResultFile resultFile,
    List<DistributionRecord> distributionResults)
  {
    // Convert the results object to a TSV string
    var config = CsvConfiguration.FromAttributes<DistributionRecord>();
    using var writer = new StringWriter();
    using var csv = new CsvWriter(writer, config);

    csv.WriteRecords(distributionResults);

    // use WithData to encode as normal
    return WithData(resultFile, writer.ToString());
  }

  /// <summary>
  /// Decode and return <see cref="ResultFile.FileData"/>
  /// </summary>
  /// <param name="resultFile">The <see cref="ResultFile"/> to decode data from.</param>
  /// <returns>The decoded contents of <see cref="ResultFile.FileData"/></returns>
  public static string DecodeData(this ResultFile resultFile)
  {
    return Encoding.UTF8.GetString(
      Convert.FromBase64String(resultFile.FileData));
  }

  /// <summary>
  /// Sets the FileName correctly based on the Analysis and Code
  /// of the job this ResultFile was produced for.
  /// </summary>
  /// <param name="resultFile">The <see cref="ResultFile"/> to set <see cref="ResultFile.FileName"/> for.</param>
  /// <param name="analysisType">The Analysis Type <see cref="CollectionAnalysisJob.Analysis"/> this file contains results for.</param>
  /// <param name="analysisCode">The Analysis Code <see cref="CollectionAnalysisJob.Code"/> this file contains results for.</param>
  /// <returns>The modified <see cref="ResultFile"/>.</returns>
  public static ResultFile WithAnalysisFileName(
    this ResultFile resultFile,
    string analysisType,
    string analysisCode)
  {
    var notImplementedMessage =
      $"Hutch RACKit does not yet support building result files for {analysisType} Analysis. Please set the filename and data manually.";
    
    resultFile.FileName = analysisType switch
    {
      AnalysisType.Distribution => analysisCode switch
      {
        DistributionCode.Generic => "code.distribution",
        DistributionCode.Demographics => "demographics.distribution",
        _ => throw new NotImplementedException(notImplementedMessage)
      },
      _ => throw new NotImplementedException(notImplementedMessage)
    };

    return resultFile;
  }
}
