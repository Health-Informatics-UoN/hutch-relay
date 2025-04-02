using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Models;

namespace Hutch.Relay.Services.JobResultAggregators;

public class DemographicsDistributionAggregator : IQueryResultAggregator
{
  public QueryResult Process(List<RelaySubTaskModel> subTasks)
  {
    throw new NotImplementedException();
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
}
