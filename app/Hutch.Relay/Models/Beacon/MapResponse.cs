using System;
using System.Text.Json.Serialization;

namespace Hutch.Relay.Models.Beacon;

public class MapResponse
{
  public required InfoMeta Meta { get; set; }

  public required MapResponseBody Response { get; set; }
}

/// <summary>
/// Map of a Beacon, its entry types and endpoints. It isconceptually similar to a website sitemap.
/// </summary>
public class MapResponseBody
{
  [JsonPropertyName("$schema")]
  public string Schema { get; } = "https://raw.githubusercontent.com/ga4gh-beacon/beacon-framework-v2/main/responses/beaconMapResponse.json";

  // Optional extra informational properties (per https://playground.rd-connect.eu/beacon2/api/map)
  public string Title { get; } = "Beacon Map";
  public string Description { get; } = "Map of a Beacon, its entry types and endpoints. It is conceptually similar to a website sitemap.";

  /// <summary>
  /// List of enpoints included in this Beacon instance. This is list is meant to inform Beacon clients, e.g. a Beacon Network, about the available endpoints, it is not used to generate any automatic list, but could be used for Beacon validation purposes.
  /// </summary>
  public required Dictionary<string, EndpointSet> EndpointSets { get; set; }
}

public class EndpointSet
{
  public required string EntryType { get; set; }

  /// <summary>
  /// The base url for this entry type. Returns a list of entries. It is added here for convenience of the Beacon clients, so they don't need to parse the OpenAPI endpoints definition to get that base endpoint. Also, in very simple Beacons, that endpoint could be the only one implemented, together with ´singleEntryUrl`, in which case the whole map of endpoints is found in the current Map.
  /// </summary>
  public required string RootUrl { get; set; }

  /// <summary>
  /// Optional, but recommended. Returns only one instance of this entry, identified by an id. It is added here for convenience of the Beacon clients, so they don't need to parse the OpenAPI endpoints definition to get that base endpoint. Also, in very simple Beacons, that endpoint could be the only one implemented, together with ´rootUrl`, in which case the whole map of endpoints is found in the current Map.
  /// </summary>
  public string? SingleEntryUrl { get; } // Relay will never provide this because it only returns aggregate granularities

  /// <summary>
  /// Optional. Returns the list of filtering terms that could be applied to this entry type. It is added here for convenience of the Beacon clients, so they don't need to parse the OpenAPI endpoints definition to get that endpoint. Also, in very simple Beacons, that endpoint could be the one of the few implemented, together with ´rootUrl and ´singleEntryUrl, in which case the whole map of endpoints is found in the current Map.
  /// </summary>
  public string? FilteringTermsUrl { get; set; }

  /// <summary>
  /// Reference to the file that includes the OpenAPI definition of the endpoints implemented in this Beacon instance. The referenced file MUST BE a valid OpenAPI definition file, as it is expected that the Beacon clients (e.g. a Beacon Network) should be able to parse it to discover additional details on the supported verbs, parameters, etc.
  /// </summary>
  public string? OpenApiEndpointsDefinition { get; set; }

  /// <summary>
  /// <para>
  /// Optional. A list describing additional endpoints implemented by this Beacon instance for that entry type. Additional details on the endpoint parameters, supported HTTP verbs, etc. could be obtained by parsing the OpenAPI definition referenced in the openAPIEndpointsDefinition attribute.</para>
  /// <para><c>returnedEntryType</c> describes which entry type is returned by querying this endpoint. It MUST match one of the entry types defined in the Beacon configuration file (beaconConfiguration.yaml).</para>
  /// </summary>
  public List<(string url, string returnedEntryType)>? Endpoints { get; }
}
