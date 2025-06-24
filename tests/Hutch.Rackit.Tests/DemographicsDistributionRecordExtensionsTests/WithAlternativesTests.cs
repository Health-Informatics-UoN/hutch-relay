using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;

namespace Hutch.Rackit.Tests.DemographicsDistributionRecordExtensionsTests;

public class WithAlternativesTests
{
  #region Test Data

  public static IEnumerable<object[]> GetData_NonSpecificallyHandledCode_EncodesAlternativesCorrectly()
  {
    yield return
    [
      new Dictionary<string, int> { ["FEMALE"] = 75, ["MALE"] = 50 },
      "^FEMALE|75^MALE|50^" // Order preserved
    ];
    yield return
    [
      new Dictionary<string, int> { ["MALE"] = 50, ["FEMALE"] = 75, ["UNKNOWN"] = 104 },
      "^MALE|50^FEMALE|75^UNKNOWN|104^"
    ];
    yield return
    [
      new Dictionary<string, int> { ["MALE"] = 50 },
      "^MALE|50^"
    ];
    yield return
    [
      new Dictionary<string, int>(),
      string.Empty
    ];
  }

  public static IEnumerable<object[]> GetData_SexCode_EncodesAlternativesCorrectly()
  {
    yield return
    [
      new Dictionary<string, int> { ["FEMALE"] = 75, ["MALE"] = 50 },
      "^MALE|50^FEMALE|75^" // Order standardised
    ];
    yield return
    [
      new Dictionary<string, int> { ["MALE"] = 50, ["FEMALE"] = 75, ["UNKNOWN"] = 104 },
      "^MALE|50^FEMALE|75^" // unexpected keys discarded
    ];
    yield return
    [
      new Dictionary<string, int> { ["MALE"] = 50 },
      "^MALE|50^FEMALE|0^" // missing keys zeroed
    ];
    yield return
    [
      new Dictionary<string, int>(),
      string.Empty
    ];
  }

  public static IEnumerable<object[]> GetData_GenomicsCode_EncodesAlternativesCorrectly()
  {
    yield return
    [
      new Dictionary<string, int> { ["No"] = 50, ["Imputed whole genome data"] = 75, ["Imputed partial genome data"] = 104 },
      "^No|50^Imputed whole genome data|75^Imputed partial genome data|104^" // unexpected keys retained
    ];
    yield return
    [
      new Dictionary<string, int> { ["Imputed whole genome data"] = 75, ["No"] = 50 },
      "^No|50^Imputed whole genome data|75^" // Order standardised - known keys first
    ];
    yield return
    [
      new Dictionary<string, int> { ["Imputed whole genome data"] = 75 },
      "^No|0^Imputed whole genome data|75^" // missing known keys zeroed
    ];
    yield return
    [
      new Dictionary<string, int>(),
      string.Empty
    ];
  }

  #endregion

  [Theory]
  [MemberData(nameof(GetData_NonSpecificallyHandledCode_EncodesAlternativesCorrectly))]
  public void NonSpecificallyHandledCode_EncodesAlternativesCorrectly(Dictionary<string, int> alternatives,
    string expected)
  {
    DemographicsDistributionRecord record = new()
    {
      Code = "TEST",
      Collection = "test_collection"
    };

    record.WithAlternatives(alternatives);

    Assert.Equal(expected, record.Alternatives);
  }

  [Theory]
  [MemberData(nameof(GetData_SexCode_EncodesAlternativesCorrectly))]
  public void SexCode_EncodesAlternativesCorrectly(Dictionary<string, int> alternatives, string expected)
  {
    DemographicsDistributionRecord record = new()
    {
      Code = Demographics.Sex,
      Collection = "test_collection"
    };

    record.WithAlternatives(alternatives);

    Assert.Equal(expected, record.Alternatives);
  }

  [Theory]
  [MemberData(nameof(GetData_GenomicsCode_EncodesAlternativesCorrectly))]
  public void GenomicsCode_EncodesAlternativesCorrectly(Dictionary<string, int> alternatives, string expected)
  {
    DemographicsDistributionRecord record = new()
    {
      Code = Demographics.Genomics,
      Collection = "test_collection"
    };

    record.WithAlternatives(alternatives);

    Assert.Equal(expected, record.Alternatives);
  }
  
  [Fact]
  public void AgeCode_ReturnsEmpty()
  {
    var alternatives = new Dictionary<string, int> { ["FEMALE"] = 75, ["MALE"] = 50 };
    
    DemographicsDistributionRecord record = new()
    {
      Code = Demographics.Age,
      Collection = "test_collection"
    };

    record.WithAlternatives(alternatives);

    Assert.Equal(string.Empty, record.Alternatives);
  }
}
