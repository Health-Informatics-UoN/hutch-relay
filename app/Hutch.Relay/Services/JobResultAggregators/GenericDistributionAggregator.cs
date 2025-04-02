using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Models;

namespace Hutch.Relay.Services.JobResultAggregators;

public class GenericDistributionAggregator(IObfuscator obfuscator) : IQueryResultAggregator
{
  private static List<GenericDistributionRecord> ParseResultFile(string tsvData)
  {
    var config = CsvConfiguration.FromAttributes<GenericDistributionRecord>();
    config.MissingFieldFound = null; // The model will initialise missing fields

    using var reader = new StringReader(tsvData);
    using var tsv = new CsvReader(reader, config);

    return tsv.GetRecords<GenericDistributionRecord>().ToList();
  }

  public QueryResult Process(List<RelaySubTaskModel> subTasks)
  {
    // Aggregation State
    (string collectionId, Dictionary<string, GenericDistributionRecord> aggregatedRecords) accumulator =
      (subTasks.FirstOrDefault()?.RelayTask.Collection ?? string.Empty, new());

    // Aggregate across all valid subtasks
    foreach (var subTask in subTasks)
    {
      if (subTask.Result is null) continue;

      var result = JsonSerializer.Deserialize<JobResult>(subTask.Result);
      // Don't crash if we can't parse the results;
      // Today we just pretend like that downstream client didn't respond and skip it
      // TODO: review this behaviour
      if (result is null) continue;

      // If the QueryResult says there's no data, jog on
      if (result.Results.Count == 0) continue;

      // Find the relevant ResultFile (if none found, this subtask will not contribute results)
      foreach (var file in result.Results.Files)
      {
        if (file.FileName != ResultFileName.CodeDistribution) continue;
        var rawFileData = file.DecodeData();

        // Check we have more than just the header row; CsvHelper won't parse it if there's no actual data
        // This could happen if the QueryResult.Count was a lie ;) or just if the file was populated weirdly
        if (rawFileData.Split("\n").Length < 2) continue;

        // If we actually have data, go ahead and parse
        var records = ParseResultFile(rawFileData);

        accumulator.AccumulateData(records);
      }
    }

    // Perform a final obfuscation pass of the now aggregated values as we convert them to a List for encoding
    var finalData = accumulator.aggregatedRecords
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
    this (string collectionId, Dictionary<string, GenericDistributionRecord> records) accumulator,
    List<GenericDistributionRecord> records)
  {
    foreach (var record in records)
    {
      if (accumulator.records.TryGetValue(record.Code, out var accumulatorRecord))
      {
        // Key was already present
        // merge Count but otherwise keep the base Accumulator record's properties
        accumulatorRecord.Count += record.Count;
      }
      else
      {
        // Create a brand new "base" record, inheriting most (but not all) from the current value
        accumulator.records.Add(
          record.Code,
          record with
          {
            // Override Collection with the Upstream ID instead of Downstream
            Collection = accumulator.collectionId,
          });
      }
    }
  }
}
