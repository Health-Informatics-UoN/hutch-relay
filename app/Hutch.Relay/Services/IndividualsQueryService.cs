using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Constants;

namespace Hutch.Relay.Services;

public class IndividualsQueryService
{
  public Task<AvailabilityJob?> CreateAvailabilityJob(List<string> queryTerms)
  {
    if (queryTerms.Count < 1)
      return Task.FromResult<AvailabilityJob?>(null);

    var rules = queryTerms
      .Select(term =>
        new Rule()
        {
          Operand = "=",
          Type = "TEXT",
          VariableName = "OMOP",
          Value = term[(term.IndexOf(':') + 1)..]
        }
      ).ToList();

    AvailabilityJob task = new()
    {
      Collection = RelayBeaconTaskDetails.Collection,
      Owner = RelayBeaconTaskDetails.Owner,
      Uuid = RelayBeaconTaskDetails.IdPrefix + Guid.NewGuid(),
      Cohort = new()
      {
        Combinator = "OR",
        Groups =
        [
          new()
          {
            Combinator = "AND",
            Rules = rules
          }
        ]
      }
    };

    return Task.FromResult<AvailabilityJob?>(task);
  }
}
