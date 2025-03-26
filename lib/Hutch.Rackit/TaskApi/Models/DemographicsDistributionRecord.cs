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
public class DemographicsDistributionRecord : IResultFileRecord
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
  [Name("MIN")]
  [Index(4)]
  public int? Min { get; set; }
  public int? Q1 { get; set; }
  [Name("MEDIAN")]
  [Index(5)]
  public int? Median { get; set; }
  [Name("MEAN")]
  [Index(6)]
  public int? Mean { get; set; }
  public int? Q3 { get; set; }
  [Name("MAX")]
  [Index(7)]
  public int? Max { get; set; }

  /// <summary>
  /// Represents the distribution of the Count across sub-values for this Code.
  /// e.g. For <see cref="Code"/> `SEX` with <see cref="Count"/> `100`
  /// <see cref="Alternatives"/> might be `^MALE|45^FEMALE|55^`.
  ///
  /// The format is `^` delimited values with a key (e.g. MALE) and count (45) pipe delimited.
  /// </summary>
  [Name("ALTERNATIVES")]
  [Index(8)]
  public string Alternatives { get; set; } = string.Empty;
  
  // TODO: What is really expected here?
  // May refer to the Table the is relative to, if not a standard ontology term
  // e.g. In Demographics Distribution, `SEX` may appear as a Code against the `person` table.
  [Name("DATASET")]
  [Index(9)]
  public string Dataset { get; set; } = string.Empty;
  
  /// <summary>
  /// Raw OMOP Code for the term. If <see cref="Code"/> is prefixed `OMOP:` the values should match.
  /// </summary>
  [Name("OMOP")]
  [Index(10)]
  public int? OmopCode { get; set; }
  
  /// <summary>
  /// OMOP Description of the term. If <see cref="Code"/> is prefixed `OMOP:` this should match <see cref="Description"/>
  /// </summary>
  [Name("OMOP_DESCR")]
  [Index(11)]
  public string OmopDescription { get; set; } = string.Empty;

  /// <summary>
  /// Category for the <see cref="Code"/>.
  /// Presumably may be defined by the ontology referenced.
  /// Also possibly some internal values e.g. <see cref="Code"/> `SEX` is <see cref="Category"/> `DEMOGRAPHICS`
  /// </summary>
  [Name("CATEGORY")]
  [Index(12)]
  public string Category { get; set; } = string.Empty;
}
