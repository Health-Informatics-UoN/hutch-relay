using Hutch.Relay.Constants;

namespace Hutch.Relay.Models.Beacon;

public class InfoResponse
{
  public required InfoMeta Meta { get; set; }

  public required InfoResponseBody Response { get; set; }
}

public class InfoResponseBody
{
  //NOTE: "Info" is an optional placeholder, so Relay omits it currently:
  // https://b2ri-documentation.readthedocs.io/en/latest/endpoints/beacon-v2.html#tag/Informational-endpoints/operation/getBeaconRoot

  public required string Id { get; set; }

  public required string Name { get; set; }

  public string ApiVersion { get; } = BeaconApiConstants.ApiVersion;

  public required string Environment { get; set; }

  public required BeaconOrganization Organization { get; set; }

  public string? Description { get; set; }

  public string? Version { get; set; }

  public string? WelcomeUrl { get; set; }

  public string? AlternativeUrl { get; set; }

  public DateTimeOffset? CreateDateTime { get; set; }

  public DateTimeOffset? UpdateDateTime { get; set; }
}
