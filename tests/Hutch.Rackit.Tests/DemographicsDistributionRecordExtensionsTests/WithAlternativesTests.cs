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
      new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase) { ["Female"] = 75, ["MALE"] = 50 },
      "^Female|75^MALE|50^" // Order, case preserved
    ];
    yield return
    [
      new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase) { ["MALE"] = 50, ["FEMALE"] = 75, ["UNKNOWN"] = 104 },
      "^MALE|50^FEMALE|75^UNKNOWN|104^"
    ];
    yield return
    [
      new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase) { ["MALE"] = 50 },
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
    // Note that successful operation in context (e.g. Relay's DemographicsDistributionAggregator)
    // requires the use of a case-insensitive keyed dictionary.
    yield return
    [
      new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase) { ["female"] = 75, ["MALE"] = 50 }, // "Incorrectly" cased inputs
      "^Male|50^Female|75^" // Order and case standardised
    ];
    yield return
    [
      new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase) { ["MALE"] = 50, ["FEMALE"] = 75, ["UNKNOWN"] = 104 },
      "^Male|50^Female|75^" // unexpected keys discarded
    ];
    yield return
    [
      new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase) { ["MALE"] = 50 },
      "^Male|50^Female|0^" // missing keys zeroed
    ];
    yield return
    [
      new Dictionary<string, int>(), // Empty Dictionary Comparer doesn't matter
      string.Empty
    ];
  }

  public static IEnumerable<object[]> GetData_GenomicsCode_EncodesAlternativesCorrectly()
  {
    yield return
    [
      new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase) { ["No"] = 50, ["Imputed whole genome data"] = 75, ["Imputed partial genome data"] = 104 },
      "^No|50^Imputed whole genome data|75^Imputed partial genome data|104^" // unexpected keys retained
    ];
    yield return
    [
      new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase) { ["Imputed whole genome data"] = 75, ["No"] = 50 },
      "^No|50^Imputed whole genome data|75^" // Order standardised - known keys first
    ];
    yield return
    [
      new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase) { ["Imputed whole genome data"] = 75 },
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

  [Fact]
  public void AlternativesDictionary_CaseSensitive_Throws()
  {
    Dictionary<string, int> alternatives = new() { ["MALE"] = 50 };

    DemographicsDistributionRecord record = new()
    {
      Code = Demographics.Sex,
      Collection = "test_collection"
    };

    Assert.Throws<ArgumentException>(() => record.WithAlternatives(alternatives));
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
    var alternatives = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase) { ["FEMALE"] = 75, ["MALE"] = 50 };
    
    DemographicsDistributionRecord record = new()
    {
      Code = Demographics.Age,
      Collection = "test_collection"
    };

    record.WithAlternatives(alternatives);

    Assert.Equal(string.Empty, record.Alternatives);
  }
}
