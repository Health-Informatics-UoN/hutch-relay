using System.Reflection;
using Hutch.Relay.Attributes;
using Hutch.Relay.Models.Beacon;

namespace Hutch.Relay.Config.Beacon;

public class BeaconInfoOptions
{
  /// <summary>
  /// A unique identifier for this Beacon installation; no fallback. Values should typically be in reverse domain notation e.g. `org.my-organization.beacon`
  /// </summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>
  /// A friendly name for this Beacon installation; falls back to Relay's Beacon name.
  /// </summary>
  public string Name { get; set; } = "Hutch Relay Beacon";

  /// <summary>
  /// Description of this Beacon installation; falls back to a generic description of Relay.
  /// </summary>
  public string Description { get; set; } = "A federated discovery service for aggregate individuals summaries across multiple downstream data sources";

  /// <summary>
  /// Details of the organisation responsible for this Beacon installation.
  /// </summary>
  public BeaconOrganization Organization { get; set; } = new();

  /// <summary>
  /// Link to a contact form, or an email address, for contacting those responsible for this Beacon installation.
  /// </summary>
  public string? ContactUrl { get; set; }

  /// <summary>
  /// Link to a web page for this Beacon installation.
  /// </summary>
  public string? WelcomeUrl { get; set; }

  /// <summary>
  /// Another URL where this Beacon may be accessed; possibly with other restrictions in place.
  /// </summary>
  public string? AlternativeUrl { get; set; }

  /// <summary>
  /// The ISO-8601 date/time this Beacon installation was made available; falls back to when Beacon functionality was made available in Relay.
  /// </summary>
  public DateTimeOffset CreatedDate { get; set; } = new(2025, 8, 21, 12, 15, 0, TimeSpan.Zero);

  /// <summary>
  /// The ISO-8601 date/time this Beacon installation was last updated; falls back to the Build timestamp of the running release of Relay.
  /// </summary>
  public DateTimeOffset? UpdatedDate { get; set; } =
    Assembly.GetAssembly(typeof(BeaconInfoOptions))
    ?.GetCustomAttribute<BuildTimestampAttribute>()
    ?.BuildTimestamp;
}
