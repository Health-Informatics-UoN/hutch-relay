namespace Hutch.Relay.Models.Beacon;

public class GA4GHServiceOrganization
{
  /// <summary>
  /// Name of the organization.
  /// </summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// URL of the website of the organization (RFC 3986 format).
  /// </summary>
  public string Url { get; set; } = string.Empty;
}
