using Hutch.Relay.Data;
using Hutch.Relay.Services;
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

      var dbManager = scope.ServiceProvider.GetRequiredService<DbManagementService>();

      await dbManager.UpdateDatabase();
    }

    // Configure the HTTP Request Pipeline
    app.UseWebPipeline();

    // Run the app!
    await app.RunAsync();
  }
}
