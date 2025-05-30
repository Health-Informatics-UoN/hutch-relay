namespace Hutch.Relay.Config;

public class MonitoringOptions
{
  /// <summary>
  /// The route to use for the health check endpoint. Defaults to `healthz` per z-pages convention.
  /// <see href="https://stackoverflow.com/a/43381061"/>
  /// </summary>
  public string HealthEndpoint { get; set; } = "/healthz";
}