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

  private string GetInfoVersion() => (string)version.EntryAssembly();

  [HttpGet("service-info")]
  public ServiceInfoResponse GetServiceInfo()
  {
    return new()
    {
      Version = GetInfoVersion(),

      Id = _options.Info.Id,
      Name = _options.Info.Name,
      Organisation = _options.Info.Organization,
      ContactUrl = _options.Info.ContactUrl,
      Description = _options.Info.Description,
      CreatedAt = _options.Info.CreatedDate,
      UpdatedAt = _options.Info.UpdatedDate,

      DocumentationUrl = _docsUrl,

      Environment = GetInfoEnvironment()
    };
  }
}
