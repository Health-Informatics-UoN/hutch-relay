namespace Hutch.Relay.Models.Beacon;

public class BeaconOrganization
{
  /// <summary>
  /// Unique identifier of the organization.
  /// </summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>
  /// Name of the organization.
  /// </summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// URL of the website of the organization (RFC 3986 format).
  /// </summary>
  public string WelcomeUrl { get; set; } = string.Empty;

  /// <summary>
  /// Description of the organization.
  /// </summary>
  public string? Description { get; set; }
  
  /// <summary>
  /// Address of the organization.
  /// </summary>
  public string? Address { get; set; }

  /// <summary>
  /// URL with the contact for the Beacon operator/maintainer, e.g. link to a contact form (RFC 3986 format) or an email (RFC 2368 format).
  /// </summary>
  public string? ContactUrl { get; set; }
  
  public string? LogoUrl { get; set; }
}
