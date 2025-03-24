using System.Globalization;
using CsvHelper.Configuration.Attributes;

namespace Hutch.Rackit.TaskApi.Models;

/// <summary>
/// A class representing a single row of a Distribution Analysis Results File (Tab separated)
/// Note that the different Distribution Analyses (e.g. GENERIC CODE vs. DEMOGRAPHICS)
/// produce files in the same structure, but use different fields for different purposes.  
/// </summary>
[Delimiter("\t")]
[CultureInfo("en")]
public class DistributionRecord
{
  /// <summary>
  /// Collection ID representing the Biobank or Dataset these results are for
  /// </summary>
  [Name("BIOBANK")]
  public string Collection { get; set; }
  
  /// <summary>
  /// Ontology code for the term in the form `&lt;ONTOLOGY&gt;:&lt;CODE&gt;` e.g. `OMOP:443614`.
  /// May appear with no prefix for internal demographics, e.g. `SEX`
  /// </summary>
  [Name("CODE")]
  public string Code { get; set; }
  
  /// <summary>
  /// Count of records in the Collection for this Code
  /// </summary>
  [Name("COUNT")]
  public int Count { get; set; }
  
  /// <summary>
  /// Description of the Code
  /// </summary>
  [Name("DESCRIPTION")]
  public string Description { get; set; }
  
  // Optional additional stats
  
  [Name("MIN")]
  public int? Min { get; set; }
  public int? Q1 { get; set; }
  [Name("MEDIAN")]
  public int? Median { get; set; }
  [Name("MEAN")]
  public int? Mean { get; set; }
  public int? Q3 { get; set; }
  [Name("MAX")]
  public int? Max { get; set; }

  /// <summary>
  /// Represents the distribution of the Count across sub-values for this Code.
  /// e.g. For <see cref="Code"/> `SEX` with <see cref="Count"/> `100`
  /// <see cref="Alternatives"/> might be `^MALE|45^FEMALE|55^`.
  ///
  /// The format is `^` delimited values with a key (e.g. MALE) and count (45) pipe delimited.
  /// </summary>
  [Name("ALTERNATIVES")]
  public string Alternatives { get; set; }
  
  // TODO: What is really expected here?
  // May refer to the Table the is relative to, if not a standard ontology term
  // e.g. In Demographics Distribution, `SEX` may appear as a Code against the `person` table.
  [Name("DATASET")]
  public string Dataset { get; set; }
  
  /// <summary>
  /// Raw OMOP Code for the term. If <see cref="Code"/> is prefixed `OMOP:` the values should match.
  /// </summary>
  [Name("OMOP")]
  public int? OmopCode { get; set; }
  
  /// <summary>
  /// OMOP Description of the term. If <see cref="Code"/> is prefixed `OMOP:` this should match <see cref="Description"/>
  /// </summary>
  [Name("OMOP_DESCR")]
  public string OmopDescription { get; set; }
  
  /// <summary>
  /// Category for the <see cref="Code"/>.
  /// Presumably may be defined by the ontology referenced.
  /// Also possibly some internal values e.g. <see cref="Code"/> `SEX` is <see cref="Category"/> `DEMOGRAPHICS`
  /// </summary>
  [Name("CATEGORY")]
  public string Category { get; set; }
}
