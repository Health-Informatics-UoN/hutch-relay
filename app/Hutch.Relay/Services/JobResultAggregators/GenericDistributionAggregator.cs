using System.Globalization;
using System.Text.Json;
using CsvHelper;
using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Models;

namespace Hutch.Relay.Services.JobResultAggregators;

public class GenericDistributionAggregator(IObfuscator obfuscator) : IQueryResultAggregator
{
  private static List<GenericDistributionRecord> ParseResultFile(string tsvData)
  {
    using var reader = new StringReader(tsvData);
    using var tsv = new CsvReader(reader, CultureInfo.InvariantCulture);

    return tsv.GetRecords<GenericDistributionRecord>().ToList();
  }

  public QueryResult Process(List<RelaySubTaskModel> subTasks)
  {
    // A Dictionary of aggregated results records keyed by Code
    Dictionary<string, GenericDistributionRecord> aggregatedRecords = new();

    // Aggregate across all valid subtasks
    foreach (var subTask in subTasks)
    {
      if (subTask.Result is null) continue;

      var result = JsonSerializer.Deserialize<JobResult>(subTask.Result);
      // Don't crash if we can't parse the results;
      // Today we just pretend like that downstream client didn't respond and skip it
      // TODO: review this behaviour
      if (result is null) continue;

      // Find the relevant ResultFile (if none found, this subtask will not contribute results)
      foreach (var file in result.Results.Files)
      {
        if (file.FileName != ResultFileName.CodeDistribution) continue;

        var records = ParseResultFile(file.DecodeData());

        aggregatedRecords.AccumulateData(records);
      }
    }

    // Perform a final obfuscation pass of the now aggregated values as we convert them to a List for encoding
    var finalData = aggregatedRecords
      .Select(x =>
        x.Value with
        {
          Count = obfuscator.Obfuscate(x.Value.Count)
        })
      .ToList();

    // TODO: Review behaviour if no subtasks were valid:
    // should we report failure Upstream instead of a 0 count?
    if (!finalData.Any())
      return new()
      {
        Count = 0,
      };

    return new()
    {
      Count = finalData.Count,
      DatasetCount = 1,
      Files =
      [
        new ResultFile
          {
            FileDescription = "code.distribution analysis results",
          }
          .WithAnalysisFileName(AnalysisType.Distribution, DistributionCode.Generic)
          .WithData(finalData)
      ]
    };
  }
}

file static class GenericDistributionAggregatorExtensions
{
  /// <summary>
  /// Accumulates additional data from a list records into an ongoing aggregated results set
  /// </summary>
  /// <param name="accumulator">The results set we are accumulating the aggregated results into.</param>
  /// <param name="records">The list of records to provide further data.</param>
  /// <returns>The updated accumulated dataset.</returns>
  public static void AccumulateData(
    this Dictionary<string, GenericDistributionRecord> accumulator,
    List<GenericDistributionRecord> records)
  {
    foreach (var record in records)
    {
      if (!accumulator.ContainsKey(record.Code))
      {
        // Create a brand new "base" record, inheriting most (but not all) from the current value
        accumulator.Add(
          record.Code,
          record with
          {
            // Override Collection with the Upstream ID instead of Downstream
            Collection = "none"
          });
      }
      else
      {
        // Key was already present
        // merge Count but otherwise keep the base Accumulator record's properties
        accumulator[record.Code].Count += record.Count;
      }
    }
  }
}
