using Hutch.Relay.Config;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Startup.Web;

public class WebInitialisationService(
  ILogger<WebInitialisationService> logger,
  IOptions<DatabaseOptions> databaseOptions,
  IOptions<RelayBeaconOptions> beaconOptions,
  IOptions<TaskApiPollingOptions> taskApiOptions,
  DbManagementService dbManagement,
  DeclarativeConfigService declarativeConfig,
  IFilteringTermsService filteringTerms,
  IQueueConnectionManager queueConnection)
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
    
    // Test Queue Backend availability
    if (!await queueConnection.IsReady())
      throw new InvalidOperationException(
        "The RelayTask Queue Backend is not ready; please check the logs and your configuration.");

    // Request Initial Beacon Filtering Terms
    if (beaconOptions.Value.Enable) await RequestBeaconFilteringTerms();

    // Report Configuration
    //   This gives CLI feedback at startup
    //   To indicate whether features are on or off
    //   or if there are any configuration warnings (or terminal errors!)
    ReportConfiguration();
  }

  private async Task RequestBeaconFilteringTerms()
  {
    var shouldRequest = beaconOptions.Value.RequestFilteringTermsOnStartup switch
    {
      StartupFilteringTermsBehaviour.IfEmpty => !await filteringTerms.Any(),
      StartupFilteringTermsBehaviour.ForceIfEmpty => !await filteringTerms.Any(),
      StartupFilteringTermsBehaviour.Always => true,
      StartupFilteringTermsBehaviour.ForceAlways => true,
      _ => false
    };
    var shouldForce = beaconOptions.Value.RequestFilteringTermsOnStartup switch
    {
      StartupFilteringTermsBehaviour.ForceIfEmpty => true,
      StartupFilteringTermsBehaviour.ForceAlways => true,
      _ => false
    };

    if (shouldRequest) await filteringTerms.RequestUpdatedTerms(shouldForce);
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

