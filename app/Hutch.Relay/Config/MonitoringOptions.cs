namespace Hutch.Relay.Config;

public class MonitoringOptions
{
  public static string Section { get; set; } = "Monitoring";

  /// <summary>
  /// The route to use for the health check endpoint. Defaults to `healthz` per z-pages convention.
  /// <see href="https://stackoverflow.com/a/43381061"/>
  /// </summary>
  public string HealthEndpoint { get; set; } = "/healthz";
}
