using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Constants;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services.IndividualsQueryServiceTests;

public class CreateAvailabilityJobTests
{
  [Fact]
  public async Task CreateAvailabilityJob_NoQueryTerms_Throws()
  {
    await Assert.ThrowsAsync<ArgumentException>(async () =>
      await IndividualsQueryService.CreateAvailabilityJob([], "test"));
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
        Value = "123",
        Category = "Condition"
      },
      new()
      {
        Type = "TEXT",
        VariableName = "OMOP",
        Operand = "=",
        Value = "456",
        Category = "Condition"
      }
    ];

    var actual = await IndividualsQueryService.CreateAvailabilityJob(terms, "test");

    Assert.NotNull(actual);
    Assert.Equal("OR", actual.Cohort.Combinator);
    Assert.Single(actual.Cohort.Groups);
    Assert.Equal("AND", actual.Cohort.Groups.First().Combinator);
    Assert.Equivalent(expectedRules, actual.Cohort.Groups.First().Rules);
  }

  [Fact]
  public async Task CreateAvailabilityJob_SetsRequiredBaseProperties()
  {
    var queueName = "test-queue";
    
    List<string> terms = ["OMOP:123", "OMOP:456"];

    var actual = await IndividualsQueryService.CreateAvailabilityJob(terms, queueName);

    Assert.NotNull(actual);

    Assert.NotEmpty(actual.Uuid);
    Assert.EndsWith(RelayBeaconTaskDetails.IdSuffix + queueName, actual.Uuid);
    Assert.IsType<Guid>(Guid.Parse(actual.Uuid.Replace(RelayBeaconTaskDetails.IdSuffix + queueName, string.Empty)));

    Assert.Equal(RelayBeaconTaskDetails.Collection, actual.Collection);
    Assert.Equal(RelayBeaconTaskDetails.Owner, actual.Owner);
  }
  
  [Theory]
  [InlineData("")]
  [InlineData("    ")]
  public async Task CreateAvailabilityJob_EmptyQueueName_Throws(string queueName)
  {
    await Assert.ThrowsAsync<ArgumentException>(async () =>
      await IndividualsQueryService.CreateAvailabilityJob(
        ["OMOP:123", "OMOP:456"], 
        queueName));
  }
}
