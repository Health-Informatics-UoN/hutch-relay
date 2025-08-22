using Hutch.Relay.Config;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Constants;
using Hutch.Relay.Models.Beacon;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Mvc;
using UoN.VersionInformation;

namespace Hutch.Relay.Controllers.Beacon;

[FeatureGate(Features.Beacon)]
[ApiController]
[Route($"{BeaconApiConstants.RoutePrefix}")]
public class InfoController(
  IOptions<RelayBeaconOptions> options,
  VersionInformationService version,
  IHostEnvironment env) : ControllerBase
{
  private readonly RelayBeaconOptions _options = options.Value;

  private const string _docsUrl = "https://hutch.health/relay";

  private string GetInfoEnvironment()
  {
    return env.EnvironmentName switch
    {
      // Map common .NET environment names to suggested Beacon values
      "Development" => "dev",
      "Production" => "prod",
      _ => env.EnvironmentName.ToLowerInvariant()
    };
  }

  private string GetInfoRelayVersion() => (string)version.EntryAssembly();

  /// <summary>
  /// Get information about the beacon using GA4GH ServiceInfo format
  /// </summary>
  /// <returns></returns>
  [HttpGet("service-info")]
  public ServiceInfoResponse GetServiceInfo()
  {
    return new()
    {
      Version = GetInfoRelayVersion(),

      Id = _options.Info.Id,
      Name = _options.Info.Name,
      Organisation = new()
      {
        Name = _options.Info.Organization.Name,
        Url = _options.Info.Organization.WelcomeUrl
      },

      ContactUrl = _options.Info.Organization.ContactUrl, // TODO: Document this change
      Description = _options.Info.Description,
      CreatedAt = _options.Info.CreatedDate,
      UpdatedAt = _options.Info.UpdatedDate,

      DocumentationUrl = _docsUrl,

      Environment = GetInfoEnvironment()
    };
  }

  /// <summary>
  /// Get information about the beacon
  /// </summary>
  /// <param name="requestedSchema">Ignored by Relay as it doesn't affect this request anyway.</param>
  /// <returns></returns>
  [HttpGet, Route(""), Route("info")]
  public InfoResponse GetRoot([FromQuery] string? requestedSchema)
  {
    return new()
    {
      Meta = new()
      {
        BeaconId = _options.Info.Id,
      },
      Response = new()
      {
        Environment = GetInfoEnvironment(),

        Id = _options.Info.Id,
        Name = _options.Info.Name,
        Organization = _options.Info.Organization,

        Description = _options.Info.Description,
        WelcomeUrl = _options.Info.WelcomeUrl,
        AlternativeUrl = _options.Info.AlternativeUrl,
        CreateDateTime = _options.Info.CreatedDate,
        UpdateDateTime = _options.Info.UpdatedDate,

        Version = GetInfoRelayVersion()
      }
    };
  }
}
