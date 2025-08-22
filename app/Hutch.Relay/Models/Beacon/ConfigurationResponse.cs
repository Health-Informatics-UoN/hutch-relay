using System.Text.Json.Serialization;
using Hutch.Relay.Config.Beacon;

namespace Hutch.Relay.Models.Beacon;

public class ConfigurationResponse
{
  public required InfoMeta Meta { get; set; }

  public required ConfigurationResponseBody Response { get; set; }
}

public class ConfigurationResponseBody
{
  [JsonPropertyName("$schema")]
  public string Schema { get; } = "https://raw.githubusercontent.com/ga4gh-beacon/beacon-framework-v2/main/responses/beaconConfigurationResponse.json";

  public required MaturityAttributes MaturityAttributes { get; set; }
  
  public required SecurityAttributes SecurityAttributes { get; set; }

  public required Dictionary<string, EntryTypeInfo> EntryTypes { get; set; }
}
