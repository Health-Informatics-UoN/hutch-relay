using Hutch.Relay.Constants;

namespace Hutch.Relay.Models.Beacon;

public class InfoMeta
{
  /// <summary>
  /// The ID of this Beacon installation.
  /// </summary>
  public required string BeaconId { get; init; }

  /// <summary>
  /// Beacon API version Relay implements
  /// </summary>
  public string ApiVersion { get; } = BeaconApiConstants.ApiVersion;

  /// <summary>
  /// Set of schemas to be used in the response to a request
  /// </summary>
  public List<ReturnedSchema> ReturnedSchemas { get; } = [];
}
