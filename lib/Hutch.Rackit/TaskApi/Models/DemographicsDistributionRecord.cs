using System.Globalization;
using System.Text;
using CsvHelper.Configuration.Attributes;

namespace Hutch.Rackit.TaskApi.Models;

/// <summary>
/// A class representing a single row of a Demographics Distribution Analysis Results File (Tab separated)
/// Note that the different Distribution Analyses (e.g. GENERIC CODE vs. DEMOGRAPHICS)
/// produce files of different (though similar) structures, and use different fields for different purposes.  
/// </summary>
[Delimiter("\t")]
[CultureInfo("en")]
[NewLine("\n")]
[Encoding("utf-8")]
public record DemographicsDistributionRecord : IResultFileRecord
{
  /// <summary>
  /// Collection ID representing the Biobank or Dataset these results are for
  /// </summary>
  [Name("BIOBANK")]
  [Index(0)]
  public required string Collection { get; set; }

  /// <summary>
  /// Ontology code for the term in the form `&lt;ONTOLOGY&gt;:&lt;CODE&gt;` e.g. `OMOP:443614`.
  /// May appear with no prefix for internal demographics, e.g. `SEX`
  /// </summary>
  [Name("CODE")]
  [Index(1)]
  public required string Code { get; set; }

  /// <summary>
  /// Description of the Code
  /// </summary>
  [Name("DESCRIPTION")]
  [Index(2)]
  public string Description { get; set; } = string.Empty;

  /// <summary>
  /// Count of records in the Collection for this Code
  /// </summary>
  [Name("COUNT")]
  [Index(3)]
  public int Count { get; set; }

  // Optional additional stats
  [Name("MIN")][Index(4)] public int? Min { get; set; }

  [Index(5)] public double? Q1 { get; set; }
  [Name("MEDIAN")][Index(6)] public double? Median { get; set; }
  [Name("MEAN")][Index(7)] public double? Mean { get; set; }

  [Index(8)] public double? Q3 { get; set; }
  [Name("MAX")][Index(9)] public double? Max { get; set; }

  /// <summary>
  /// <para>
  /// Represents the distribution of the Count across sub-values for this Code.
  /// e.g. For <see cref="Code"/> <c>SEX</c> with <see cref="Count"/> <c>100</c>
  /// <see cref="Alternatives"/> might be <c>^MALE|45^FEMALE|55^</c>.
  /// </para>
  /// <para>
  /// The format is <c>^</c> delimited values with a key (e.g. MALE) and count (45) pipe delimited.
  /// </para>
  /// <para>
  /// It's unknown if order (or presence) of keys is important: e.g. are <c>^MALE|100^</c> or <c>^FEMALE|70^MALE|30^</c> valid?
  /// </para>
  /// </summary>
  [Name("ALTERNATIVES")]
  [Index(10)]
  public string Alternatives { get; set; } = string.Empty;

  // TODO: What is really expected here?
  // May refer to the Table the is relative to, if not a standard ontology term
  // e.g. In Demographics Distribution, `SEX` may appear as a Code against the `person` table.
  [Name("DATASET")][Index(11)] public string Dataset { get; set; } = string.Empty;

  /// <summary>
  /// Raw OMOP Code for the term. If <see cref="Code"/> is prefixed `OMOP:` the values should match.
  /// </summary>
  [Name("OMOP")]
  [Index(12)]
  public int? OmopCode { get; set; }

  /// <summary>
  /// OMOP Description of the term. If <see cref="Code"/> is prefixed `OMOP:` this should match <see cref="Description"/>
  /// </summary>
  [Name("OMOP_DESCR")]
  [Index(13)]
  public string OmopDescription { get; set; } = string.Empty;

  /// <summary>
  /// Category for the <see cref="Code"/>.
  /// Presumably may be defined by the ontology referenced.
  /// Also possibly some internal values e.g. <see cref="Code"/> `SEX` is <see cref="Category"/> `DEMOGRAPHICS`
  /// </summary>
  [Name("CATEGORY")]
  [Index(14)]
  public string Category { get; set; } = string.Empty;
}

public static class DemographicsDistributionRecordExtensions
{
  /// <summary>
  /// A lookup by <see cref="Code"/> that provides the expected keys and their order for the <see cref="Alternatives"/> property.
  /// </summary>
  private static readonly Dictionary<string, string> _alternativeKeys = new()
  {
    [Demographics.Sex] = "MALE,FEMALE",
    [Demographics.Genomics] = "No,*" // `*` Allows unknown keys to be appended
  };

  public static DemographicsDistributionRecord WithAlternatives(
    this DemographicsDistributionRecord record,
    Dictionary<string, int> alternatives)
  {
    // early return cases if nothing to do
    if (alternatives.Count == 0 || record.Code == Demographics.Age) // AGE doesn't use alternatives
    {
      record.Alternatives = string.Empty;
      return record;
    }

    // See if this is a key we know the expected alternatives for
    string[] knownAlternatives = _alternativeKeys.GetValueOrDefault(record.Code)?.Split(",") ?? [];
    var allowUnknownAlternatives = knownAlternatives is [];

    if (knownAlternatives.Length > 0)
    {
      // We know the expected keys / order for this Code
      record.Alternatives = knownAlternatives
        .Aggregate("^", (current, k) =>
          {
            if (k == "*") // * isn't a real Key, so don't add it, but change the behaviour
            {
              allowUnknownAlternatives = true;
              return current;
            }

            return current + $"{k}|{alternatives.GetValueOrDefault(k)}^";
          });
    }

    // Either the Code is unknown, or a known Code supports unknown alternatives (`*`)
    if (allowUnknownAlternatives)
    {
      // We just set the values we don't already have as they come
      record.Alternatives = alternatives
        .Aggregate(
          string.IsNullOrWhiteSpace(record.Alternatives)
            ? "^"
            : record.Alternatives,
          (current, item) =>
          {
            // Skip keys we already got from knownAlternatives
            if (knownAlternatives.Contains(item.Key))
              return current;

            return current + $"{item.Key}|{item.Value}^";
          });
    }

    return record;
  }

  public static Dictionary<string, int> GetAlternatives(this DemographicsDistributionRecord record)
  {
    if (string.IsNullOrWhiteSpace(record.Alternatives)) return [];

    Dictionary<string, int> alternatives = new();

    foreach (var kv in record.Alternatives.Substring(1, record.Alternatives.Length - 2).Split("^"))
    {
      if (kv.Split("|") is not [var k, var v]) continue;

      if (int.TryParse(v, out var i))
        alternatives[k] = i;
      else throw new FormatException($"{alternatives[k]} for {k} is not a valid integer.");
    }

    return alternatives;
  }
}
