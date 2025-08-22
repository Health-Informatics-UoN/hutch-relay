namespace Hutch.Relay.Models.Beacon;

public class ServiceInfoResponse
{
  public required string Id { get; set; }
  public required string Name { get; set; }
  public GA4GHServiceType Type { get; set; } = new();
  public string? Description { get; set; }
  public required BeaconOrganization Organisation { get; set; }
  public string? ContactUrl { get; set; }
  public string? DocumentationUrl { get; set; }
  public DateTimeOffset? CreatedAt { get; set; }
  public DateTimeOffset? UpdatedAt { get; set; }
  public string? Environment { get; set; }
  public required string Version { get; set; }
}
