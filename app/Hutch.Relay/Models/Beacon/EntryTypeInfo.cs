using System.Text.Json.Serialization;

namespace Hutch.Relay.Models.Beacon;

public class EntryTypeInfo
{
  /// <summary>
  /// Refers to the JSON Schema which describes the set of valid attributes for this particular document type. This attribute is mostly used in schemas that should be tested in Beacon implementations.
  /// </summary>
  [JsonPropertyName("$schema")]
  public string? Schema { get; } = "https://raw.githubusercontent.com/ga4gh-beacon/beacon-framework-v2/main/configuration/entryTypeDefinition.json";

  /// <summary>
  /// A (unique) identifier of the element.
  /// </summary>
  public required string Id { get; set; }

  /// <summary>
  /// A distinctive name for the element.
  /// </summary>
  public required string Name { get; set; }

  /// <summary>
  /// Definition of an ontology term.
  /// </summary>
  public required BeaconOntologyTermDefinition OntologyTermForThisType { get; set; }

  /// <summary>
  /// This is label to group together entry types that are part of the same specification.
  /// </summary>
  public required string PartOfSpecification { get; set; }

  /// <summary>
  /// Definition of the basic element, which is root for most other objects.
  /// </summary>
  public required BeaconSchemaDefinition DefaultSchema { get; set; }

  /// <summary>
  /// A textual description for the element.
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// List of additional schemas that could be used for this concept in this instance of Beacon.
  /// </summary>
  public List<BeaconSchemaDefinition>? AdditionallySupportedSchemas { get; set; }

  /// <summary>
  /// If the entry type is a collection of other entry types, (e.g. a Dataset is a collection of Records), then this attribute must list the entry types that could be included. One collection type could be defined as included more than one entry type (e.g. a Dataset could include Individuals or Genomic Variants), in such cases the entries are alternative, meaning that a given instance of this entry type could be of only one of the types (e.g. a given Dataset contains Individuals, while another Dataset could contain Genomic Variants, but not both at once).
  /// </summary>
  public object? CollectionOf { get; set; }

  /// <summary>
  /// Reference to the file with the list of filtering terms that could be used to filter this concept in this instance of Beacon. The referenced file could be used to populate the filteringTermsendpoint. Having it independently should allow for updating the list of accepted filtering terms when it is necessary.
  /// </summary>
  public string? FilteringTerms { get; } // Aways irrelevant to Relay since there is no file we can refer to for the source of the entrytype's filtering terms

  /// <summary>
  /// Switch that declares if this Beacon instance, for a given entry type, admits queries that does not include any element that restrict returning all the results, like filters, parameters, ids, etc. The value is always 'false' for Relay.
  /// </summary>
  public bool? NonFilteredQueriesAllowed { get; } = false;
}

/// <summary>
/// Definition of an ontology term.
/// </summary>
public class BeaconOntologyTermDefinition
{
  /// <summary>
  /// A CURIE identifier, e.g. as id for an ontology term.
  /// </summary>
  //  validation: (CURIE) ^\w[^:]+:.+$
  public required string Id { get; set; }

  /// <summary>
  /// The text that describes the term. By default it could be the preferred text of the term, but is it acceptable to customize it for a clearer description and understanding of the term in an specific context.
  /// </summary>
  public string? Label { get; set; }
}

/// <summary>
/// An annotated URL address or a file reference
/// </summary>
public class BeaconSchemaDefinition
{
  /// <summary>
  /// A (unique) identifier of the element.
  /// </summary>
  public required string Id { get; set; }

  /// <summary>
  /// A distinctive name for the element.
  /// </summary>
  public required string Name { get; set; }

  public required string ReferenceToSchemaDefinition { get; set; }

  /// <summary>
  /// A textual description for the element.
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// this version reference is only used for readability in the client. The <see cref="ReferenceToSchemaDefinition"/> property is the only source for determining the actual schema used.
  /// </summary>
  public string? SchemaVersion { get; set; }
}
