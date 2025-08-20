using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services;
using Xunit;

namespace Hutch.Relay.Tests.Services.FilteringTermsServiceTests;

public class TermsMappingTests
{

  [Fact]
  public void Map_SetsTermProperties()
  {
    var distributionRecord = new GenericDistributionRecord()
    {
      Code = "OMOP:12345",
      Collection = "",
      Count = 12345,
      Category = "Condition",
      OmopDescription = "My Fictional OMOP Term",
      OmopCode = 12345
    };

    var expected = new FilteringTerm()
    {
      Term = distributionRecord.Code,
      SourceCategory = distributionRecord.Category,
      Description = distributionRecord.OmopDescription,
    };

    var actual = FilteringTermsService.Map(distributionRecord);

    Assert.Equivalent(expected, actual);
  }

  [Theory]
  [InlineData(CodeCategory.Condition, null)]
  [InlineData(CodeCategory.Measurement, null)]
  [InlineData(CodeCategory.Observation, null)]
  [InlineData(CodeCategory.Ethnicity, "Person")]
  [InlineData(CodeCategory.Race, "Person")]
  [InlineData(CodeCategory.Gender, "Person")]
  public void Map_SetsVarCat(string category, string? varcat)
  {
    var distributionRecord = new GenericDistributionRecord()
    {
      Code = "OMOP:12345",
      Collection = "",
      Count = 12345,
      Category = category,
      OmopDescription = "My Fictional OMOP Term",
      OmopCode = 12345
    };

    var actual = FilteringTermsService.Map(distributionRecord);

    Assert.Equal(varcat, actual.VarCat);
  }

  [Theory]
  [InlineData("Description", "OmopDescription", "OmopDescription")]
  [InlineData("", "OmopDescription", "OmopDescription")]
  [InlineData("Description", "", "Description")]
  [InlineData("", "", "")]
  public void Map_PrefersOmopDescription(string description, string omopDescription, string expected)
  {
    var distributionRecord = new GenericDistributionRecord()
    {
      Code = "OMOP:12345",
      Collection = "",
      Count = 12345,
      Category = "Measurement",
      Description = description,
      OmopDescription = omopDescription,
      OmopCode = 12345
    };

    var actual = FilteringTermsService.Map(distributionRecord);

    Assert.Equal(expected, actual.Description);
  }

  [Fact]
  public void Map_MapsList()
  {
    List<GenericDistributionRecord> distributionRecords = [
      new()
      {
        Code = "OMOP:12345",
        Collection = "",
        Count = 12345,
        Category = "Observation",
        OmopDescription = "An Observation",
        OmopCode = 12345
      },
      new()
      {
        Code = "OMOP:789",
        Collection = "",
        Count = 16,
        Category = "Gender",
        Description = "Female",
        OmopCode = 789
      },
      new()
      {
        Code = "OMOP:54321",
        Collection = "",
        Count = 100,
        Category = "Condition",
        Description = "A Condition",
        OmopDescription = "An OMOP Condition",
        OmopCode = 54321
      }
    ];

    List<FilteringTerm> expected = [
      new() {
        Term = "OMOP:12345",
        SourceCategory = "Observation",
        Description = "An Observation",
      },
      new() {
        Term = "OMOP:789",
        SourceCategory = "Gender",
        Description = "Female",
        VarCat = "Person"
      },
      new() {
        Term = "OMOP:54321",
        SourceCategory = "Condition",
        Description = "An OMOP Condition",
      }
    ];

    var actual = FilteringTermsService.Map(distributionRecords);

    Assert.Collection(actual,
      x => Assert.Equivalent(expected[0], x),
      x => Assert.Equivalent(expected[1], x),
      x => Assert.Equivalent(expected[2], x));
  }
}
