namespace Hutch.Relay.Models.Beacon;

public class ServiceInfoResponse
{
  public required string Id { get; set; }
  public required string Name { get; set; }
  public GA4GHServiceType Type { get; set; } = new();
  public string? Description { get; set; }
  public required GA4GHServiceOrganization Organisation { get; set; }

  /// <summary>
  /// URL with the contact for the Beacon operator/maintainer, e.g. link to a contact form (RFC 3986 format) or an email (RFC 2368 format).
  /// </summary>
  public string? ContactUrl { get; set; }
  public string? DocumentationUrl { get; set; }
  public DateTimeOffset? CreatedAt { get; set; }
  public DateTimeOffset? UpdatedAt { get; set; }
  public string? Environment { get; set; }
  public required string Version { get; set; }
}
