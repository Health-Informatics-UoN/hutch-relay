using Hutch.Rackit.TaskApi.Models;

namespace Hutch.Rackit.Tests.DemographicsDistributionRecordExtensionsTests;

public class GetAlternativesTests
{
  public static IEnumerable<object[]> GetData_ValidValues_DecodeCorrectly()
  {
    yield return
    [
      "^FEMALE|75^MALE|50^",
      new Dictionary<string, int> { ["FEMALE"] = 75, ["MALE"] = 50 },
    ];
    yield return
    [
      "^MALE|50^FEMALE|75^UNKNOWN|104^",
      new Dictionary<string, int> { ["MALE"] = 50, ["FEMALE"] = 75, ["UNKNOWN"] = 104 },
    ];
    yield return
    [
      "^MALE|50^",
      new Dictionary<string, int> { ["MALE"] = 50 },
    ];
  }

  [Fact]
  public void EmptyString_ReturnsEmptyResult()
  {
    DemographicsDistributionRecord record = new()
    {
      Collection = "test_collection",
      Code = "SEX"
    };

    var actual = record.GetAlternatives();

    Assert.Empty(actual);
  }

  [Theory]
  [MemberData(nameof(GetData_ValidValues_DecodeCorrectly))]
  public void ValidValues_DecodeCorrectly(string value, Dictionary<string, int> expected)
  {
    DemographicsDistributionRecord record = new()
    {
      Collection = "test_collection",
      Code = "SEX",
      Alternatives = value
    };

    var actual = record.GetAlternatives();

    Assert.Equivalent(expected, actual);
  }
}
