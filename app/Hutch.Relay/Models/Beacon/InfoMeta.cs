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
  // Given Relay's granularity, this will always be empty currently as we never return entities in responses.
  public List<ReturnedSchema> ReturnedSchemas { get; } = [];
}
