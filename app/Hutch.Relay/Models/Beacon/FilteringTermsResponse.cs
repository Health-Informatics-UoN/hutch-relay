namespace Hutch.Relay.Models.Beacon;

public class FilteringTermsResponse
{
  public required InfoMeta Meta { get; set; }

  /// <summary>
  /// Filtering terms and ontology resources utilised in this Beacon.
  /// </summary>
  public required FilteringTermsResponseBody Response { get; set; }
}

public class FilteringTermsResponseBody
{
  public List<FilteringTerm>? FilteringTerms { get; set; } = [];

  /// <summary>
  /// Ontology resources defined externally to this beacon implementation
  /// </summary>
  // Relay doesn't currently support this; but perhaps we should given we know we're using OMOP?
  public List<object>? Resources { get; set; } // TODO: Add model when (if?) implementing
}
