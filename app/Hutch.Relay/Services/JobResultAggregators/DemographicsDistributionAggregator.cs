using System.Text.Json;
using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config;
using Hutch.Relay.Models;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Services.JobResultAggregators;

public class DemographicsDistributionAggregator(IObfuscator obfuscator) : IQueryResultAggregator
{
  public QueryResult Process(List<RelaySubTaskModel> subTasks)
  {
    DemographicsAccumulator accumulator = new(subTasks.FirstOrDefault()?.RelayTask.Collection ?? string.Empty);

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
        if (file.FileName != ResultFileName.DemographicsDistribution) continue;
        var rawFileData = file.DecodeData();

        // Check we have more than just the header row; CsvHelper won't parse it if there's no actual data
        // This could happen if the QueryResult.Count was a lie ;) or just if the file was populated weirdly
        if (rawFileData.Split("\n").Length < 2) continue;

        // If we actually have data, go ahead and parse
        var records = ResultFileHelpers.ParseFileData<DemographicsDistributionRecord>(rawFileData);

        accumulator.AccumulateData(records);
      }
    }

    var finalData = accumulator.FinaliseAggregation(obfuscator);

    if (!finalData.Any())
      return new()
      {
        Count = 0
      };
    
    return new()
    {
      Count = finalData.Count,
      DatasetCount = 1,
      Files =
      [
        new ResultFile
          {
            FileDescription = "demographics.distribution analysis results",
          }
          .WithAnalysisFileName(AnalysisType.Distribution, DistributionCode.Demographics)
          .WithData(finalData)
      ]
    };
  }
}

internal record AlternativesAccumulator(DemographicsDistributionRecord BaseRecord)
{
  public Dictionary<string, List<int>> Alternatives { get; init; } = [];
}

internal record DemographicsAccumulator(string CollectionId)
{
  public Dictionary<string, AlternativesAccumulator> Alternatives { get; init; } = [];
}

internal static class DemographicsDistributionAggregatorExtensions
{
  // TODO: More work will be required to aggregate some other demographics such as AGE
  // but this requires decisions on e.g. how to obfuscate those results.
  // The reality is Bunny does not do these types today, so neither does Relay.
  // Should Relay exclude these types today to avoid misrepresentation if it were provided them?
  // Do we know exhaustively the demographics codes we can/can't do?
  public static DemographicsAccumulator AccumulateData(
    this DemographicsAccumulator accumulator,
    List<DemographicsDistributionRecord> records)
  {
    foreach (var record in records)
    {
      if (record.Code == "AGE") continue;

      if (accumulator.Alternatives.TryGetValue(record.Code, out var alternativesAccumulator))
      {
        // Key was already present

        // Add Alternatives
        var alternatives = record.GetAlternatives();

        foreach (var (alternative, value) in alternatives)
        {
          if (!alternativesAccumulator.Alternatives.ContainsKey(alternative))
            alternativesAccumulator.Alternatives[alternative] = [];
          alternativesAccumulator.Alternatives[alternative].Add(value);
        }
      }
      else
      {
        // Create a brand new coded entry, with a base record we can use later to populate final properties
        accumulator.Alternatives.Add(
          record.Code,
          new(record)
          {
            Alternatives = record.GetAlternatives()
              .Select(x => (x.Key, new List<int> { x.Value }))
              .ToDictionary()
          });
      }
    }

    return accumulator;
  }

  public static List<DemographicsDistributionRecord> FinaliseAggregation(
    this DemographicsAccumulator accumulator,
    IObfuscator? obfuscator = null)
  {
    obfuscator ??= new Obfuscator(Options.Create(new ObfuscationOptions()));

    List<DemographicsDistributionRecord> records = [];

    foreach (var (code, value) in accumulator.Alternatives)
    {
      // sum and obfuscate our accumulated dat, by alternative key
      var aggregateAlternatives = value.Alternatives.ToDictionary(
        x => x.Key,
        x => obfuscator.Obfuscate(x.Value.Sum()));

      // sum the total across all obfuscated alternatives
      var aggregateCount = aggregateAlternatives.Values.Sum();

      // buildup the final record
      var record = value.BaseRecord with
      {
        Collection = accumulator.CollectionId,
        Count = aggregateCount,
      };

      record.WithAlternatives(aggregateAlternatives);

      records.Add(record);
    }

    return records;
  }
}
