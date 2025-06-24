using System.IO.Compression;
using System.Text.Json;
using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config;
using Hutch.Relay.Models;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Services.JobResultAggregators;

public class DemographicsDistributionAggregator(IObfuscator obfuscator) : IQueryResultAggregator
{
  public static DemographicsDistributionRecord GetBaseGenomicsRecord(string collectionId) => new()
  {
    Code = Demographics.Genomics,
    Description = "Genomics",
    Collection = collectionId,
    Count = 0,
    Alternatives = "^No|0^",
    Dataset = "person",
    Category = "Demographics"
  };

  public QueryResult Process(string collectionId, List<RelaySubTaskModel> subTasks)
  {
    DemographicsAccumulator accumulator = new(collectionId);

    // Always initialise with an empty GENOMICS row as RQuest UI expects it; Results data can supplement it as necessary
    // TODO: also need to aggregate correctly into the Genomics row under "No" if Genomics are missing?
    accumulator.Alternatives.Add(Demographics.Genomics,
      new(GetBaseGenomicsRecord(collectionId))
      {
        Alternatives = {
          ["No"] = [0]
        }
      });

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

    // NOTE: Empty data is impossible for Demographics since at a minimum we always populate an empty Genomics row

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
    // If a `Genomics` record is not provided
    // We will fabricate one by taking the largest provided record count
    // (realistically all record totals should match anyway)
    // and recording that as "No" genomic data
    var genomicsFound = false;
    var largestTotal = 0;

    foreach (var record in records)
    {
      if (record.Code == Demographics.Age) continue;
      if (record.Code == Demographics.Genomics) genomicsFound = true; // We'll use the provided record

      // calculate record total (in case we need it) by summing alternatives
      var recordTotal = 0;

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
          recordTotal += value;
        }
      }
      else
      {
        // Create a brand new coded entry, with a base record we can use later to populate final properties
        alternativesAccumulator = new(record)
        {
          Alternatives = record.GetAlternatives()
              .Select(x => (x.Key, new List<int> { x.Value }))
              .ToDictionary()
        };

        accumulator.Alternatives.Add(
          record.Code, alternativesAccumulator);

        recordTotal = alternativesAccumulator.Alternatives.Values.Sum(x => x.Sum());
      }

      // update largest total in case we need it
      if (record.Count > recordTotal) recordTotal = record.Count; // use the reported count if it exceeds the alternatives sum?
      if (recordTotal > largestTotal) largestTotal = recordTotal;
    }

    if (!genomicsFound)
    {
      // Add our calculated count to the GENOMICS "No" count.
      // (initialising the Genomics record along the way if necessary)
      const string NO_KEY = "No";

      if (!accumulator.Alternatives.ContainsKey(Demographics.Genomics))
        accumulator.Alternatives[Demographics.Genomics] = new(DemographicsDistributionAggregator.GetBaseGenomicsRecord(accumulator.CollectionId));

      var alternativesAccumulator = accumulator.Alternatives[Demographics.Genomics];

      if (!alternativesAccumulator.Alternatives.ContainsKey(NO_KEY))
        alternativesAccumulator.Alternatives[NO_KEY] = [];

      alternativesAccumulator.Alternatives[NO_KEY].Add(largestTotal);
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
      // sum and obfuscate our accumulated data, by alternative key
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
