using Hutch.Relay.Config;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Services;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Startup.Web;

public class WebInitialisationService(
  IOptions<DatabaseOptions> databaseOptions,
  IOptions<RelayBeaconOptions> beaconOptions,
  IOptions<TaskApiPollingOptions> taskApiOptions,
  DbManagementService dbManagement,
  DeclarativeConfigService declarativeConfig,
  ILogger<WebInitialisationService> logger)
{
  /// <summary>
  /// Perform any initialisation tasks
  /// </summary>
  public async Task Initialise()
  {
    // Add any initialisation code here <3

    // Make migrations
    if (databaseOptions.Value.ApplyMigrationsOnStartup)
    {
      await dbManagement.UpdateDatabase();
    }

    // Resolve Declarative Config
    await declarativeConfig.ReconcileDownstreamUsers();

    // Report Configuration
    //   This gives CLI feedback at startup
    //   To indicate whether features are on or off
    //   or if there are any configuration warnings (or terminal errors!)
    ReportConfiguration();
  }

  private void ReportConfiguration()
  {
    LogBooleanConfig(beaconOptions.Value.Enable, "GA4GH Beacon API", v => v ? "is Enabled" : "is Disabled");
    LogBooleanConfig(taskApiOptions.Value.Enable, "Upstream Task API", v => v ? "is Enabled" : "is Disabled");
  }

  private void LogBooleanConfig(bool configValue, string name, Func<bool, string>? labelSelector)
  {
    var label = labelSelector?.Invoke(configValue) ?? "";
    var icon = configValue ? "✅" : "❌";

    logger.LogInformation("{Icon} {Name} {Label}", icon, name, label);
  }

  private void LogConfigWarning(string name, string warning)
  {
    logger.LogWarning("⚠️ Configuration Warning! {Name}: {Label}", name, warning);
  }
}

public static class WebInitialisationExtensions
{
  /// <summary>
  /// Initialise a web app by instantiating a scoped <see cref="WebInitialisationService"/> to resolve dependencies
  /// and then running <see cref="WebInitialisationService.Initialise"/>
  /// </summary>
  /// <param name="app">The web application to initialise.</param>
  /// <exception cref="InvalidOperationException"></exception>
  public static async Task Initialise(this WebApplication app)
  {
    using var scope = app.Services.CreateScope();

    if (scope is null)
      throw new InvalidOperationException("Service Configuration failure.");

    await scope.ServiceProvider.GetRequiredService<WebInitialisationService>()
      .Initialise();
  }
}

