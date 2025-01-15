using Hutch.Relay.Data;
using Microsoft.EntityFrameworkCore;

namespace Hutch.Relay.Startup.Web;

public static class WebEntrypoint
{
  public static async Task Run(string[] args)
  {
    var b = WebApplication.CreateBuilder(args);

    // Configure DI Services
    b.ConfigureServices();

    // Build the app
    var app = b.Build();
    
    // Make migrations
    if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
    {
      using var scope = app.Services.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      try
      {
        await dbContext.Database.MigrateAsync();
      }
      catch (Exception ex)
      {
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("MigrationLogger");
        logger.LogError(ex, "An error occurred while applying database migrations.");
        throw;
      }
    }

    // Configure the HTTP Request Pipeline
    app.UseWebPipeline();

    // Run the app!
    await app.RunAsync();
  }
}
