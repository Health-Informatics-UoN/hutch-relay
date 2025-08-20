using System.ComponentModel.DataAnnotations;

namespace Hutch.Relay.Models;

public class CachedFilteringTerm
{
  [Key]
  public required string Term { get; set; }

  /// <summary>
  /// Category for the term as specified by the datasource.
  /// 
  /// In Task API Generic Code Distribution this will be e.g. `Gender`, `Condition`, etc.
  /// </summary>
  public required string SourceCategory { get; set; }

  /// <summary>
  /// What we mapped the SourceCategory to,
  /// for Task API Availability Queries' `varcat` field, e.g. `Gender` -> `person`.
  /// Note that the mapping is defined by RACKit.
  /// Unmapped categories should revert to SourceCategory.
  /// </summary>
  public string? VarCat { get; set; }
  
  /// <summary>
  /// Optional Term description, e.g. OMOP Description might be provided by Task API
  /// </summary>
  public string? Description { get; set; }
}
