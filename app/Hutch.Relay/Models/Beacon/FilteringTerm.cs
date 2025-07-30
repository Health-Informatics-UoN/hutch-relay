namespace Hutch.Relay.Models.Beacon;

public class FilteringTerm
{

  /// <summary>
  /// <para>The type of term.</para>
  /// <para>In practice we only support `ontologyTerm` here today due to the terms source being downstream Task API Code Distribution</para>
  /// </summary>
  public string Type { get; set; } = "ontologyTerm";

  /// <summary>
  /// The actual filtering term, e.g. a prefixed OMOP code: `OMOP:8507`
  /// </summary>
  public required string Id { get; set; }

  /// <summary>
  /// Optional friendly description or label, e.g. the full description of an OMOP term
  /// </summary>
  public string? Label { get; set; }
}
