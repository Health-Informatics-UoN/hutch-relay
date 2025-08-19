using Hutch.Relay.Config.Beacon;

namespace Hutch.Relay.Models.Beacon;

public class Meta
{
  public string BeaconId { get; set; } = string.Empty;

  public string ApiVersion { get; set; } = "v2.0";
  
  //public List<ReturnedSchema> ReturnedSchemas { get; set; } = new();
  
  public string ReturnedGranularity { get; set; } = nameof(Granularity.boolean);
  
  //public RequestSummary ReceivedRequestSummary { get; set; } = new();
}
