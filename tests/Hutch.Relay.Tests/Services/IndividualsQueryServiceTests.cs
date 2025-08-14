using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Constants;
using Hutch.Relay.Services;
using Xunit;

namespace Hutch.Relay.Tests.Services;

public class IndividualsQueryServiceTests
{
  [Fact]
  public async Task CreateAvailabilityJob_NoQueryTerms_ReturnsNull()
  {
    var individuals = new IndividualsQueryService();
    var result = await individuals.CreateAvailabilityJob([]);

    Assert.Null(result);
  }

  [Fact]
  public async Task CreateAvailabilityJob_QueryTerms_ReturnsJobWithTermRules()
  {
    List<string> terms = ["OMOP:123", "OMOP:456"];

    List<Rule> expectedRules =
    [
      new()
      {
        Type = "TEXT",
        VariableName = "OMOP",
        Operand = "=",
        Value = "123"
      },
      new()
      {
        Type = "TEXT",
        VariableName = "OMOP",
        Operand = "=",
        Value = "456"
      }
    ];

    var individuals = new IndividualsQueryService();
    var actual = await individuals.CreateAvailabilityJob(terms);

    Assert.NotNull(actual);
    Assert.Equal("OR", actual.Cohort.Combinator);
    Assert.Single(actual.Cohort.Groups);
    Assert.Equal("AND", actual.Cohort.Groups.First().Combinator);
    Assert.Equivalent(expectedRules, actual.Cohort.Groups.First().Rules);
  }

  [Fact]
  public async Task CreateAvailabilityJob_SetsRequiredBaseProperties()
  {
    List<string> terms = ["OMOP:123", "OMOP:456"];

    var individuals = new IndividualsQueryService();
    var actual = await individuals.CreateAvailabilityJob(terms);

    Assert.NotNull(actual);

    Assert.NotEmpty(actual.Uuid);
    Assert.StartsWith(RelayBeaconTaskDetails.IdPrefix, actual.Uuid);
    Assert.IsType<Guid>(Guid.Parse(actual.Uuid.Replace(RelayBeaconTaskDetails.IdPrefix, string.Empty)));

    Assert.Equal(RelayBeaconTaskDetails.Collection, actual.Collection);
    Assert.Equal(RelayBeaconTaskDetails.Owner, actual.Owner);
  }
}
