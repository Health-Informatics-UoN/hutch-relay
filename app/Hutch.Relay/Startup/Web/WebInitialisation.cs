using Hutch.Relay.Config;
using Hutch.Relay.Services;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Startup.Web;

public class WebInitialisationService(
  IOptions<DatabaseOptions> databaseOptions,
  DbManagementService dbManagement,
  DeclarativeConfigService declarativeConfig)
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

