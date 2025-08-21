using System.Reflection;
using Hutch.Relay.Attributes;
using Hutch.Relay.Models.Beacon;

namespace Hutch.Relay.Config.Beacon;

public class BeaconInfoOptions
{
  public string Id { get; set; } = string.Empty;

  public string Name { get; set; } = "Hutch Relay Beacon";

  public string Description { get; set; } = "A federated discovery service for aggregate individuals summaries across multiple downstream data sources";

  public BeaconOrganization Organization { get; set; } = new();

  public string? ContactUrl { get; set; }

  public DateTimeOffset CreatedDate { get; set; } = new(2025, 8, 21, 12, 15, 0, TimeSpan.Zero);

  public DateTimeOffset? UpdatedDate { get; set; } =
    Assembly.GetAssembly(typeof(BeaconInfoOptions))
    ?.GetCustomAttribute<BuildTimestampAttribute>()
    ?.BuildTimestamp;
}
