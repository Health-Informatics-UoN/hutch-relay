using Hutch.Relay.Config;
using Hutch.Relay.Data;
using Hutch.Relay.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

    // Perform additional initialisation before we run the Web App
    await app.Initialise();

    // Configure the HTTP Request Pipeline
    app.UseWebPipeline();

    // Run the app!
    await app.RunAsync();
  }
}
