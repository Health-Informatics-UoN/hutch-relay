using System.Reflection;
using Hutch.Relay.Attributes;

namespace Hutch.Relay.Config.Beacon;

public class BeaconInfoOptions
{
  private static readonly DateTimeOffset _relayBeaconCreatedDate = new(2025, 8, 22, 0, 0, 0, TimeSpan.Zero);

  public string Id { get; set; } = string.Empty;

  public string Name { get; set; } = "Hutch Relay Beacon";

  public string Description { get; set; } = "A federated discovery service for aggregate individuals summaries across multiple downstream data sources";

  public BeaconOrganization Organization { get; set; } = new();

  public string? ContactUrl { get; set; }

  public DateTimeOffset CreatedDate { get; set; } = _relayBeaconCreatedDate;

  public DateTimeOffset UpdatedDate { get; set; } =
    Assembly.GetAssembly(typeof(BeaconInfoOptions))
    ?.GetCustomAttribute<BuildTimestampAttribute>()
    ?.BuildTimestamp ?? _relayBeaconCreatedDate;
}

public class BeaconOrganization
{
  public string Name { get; set; } = string.Empty;

  public string Url { get; set; } = string.Empty;
}
