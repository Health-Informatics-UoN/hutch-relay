using Hutch.Relay.Config;
using Microsoft.Extensions.Options;
using Serilog;

namespace Hutch.Relay.Startup.Web;

public static class ConfigureWebPipeline
{
  /// <summary>
  /// Configure the HTTP Request Pipeline for an ASP.NET Core app
  /// </summary>
  /// <param name="app"></param>
  /// <returns></returns>
  public static WebApplication UseWebPipeline(this WebApplication app)
  {
    var monitoringOptions = app.Services.GetRequiredService<IOptions<MonitoringOptions>>().Value;

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapUonVersionInformation();

    app.UseAuthentication();
    app.UseAuthorization();
    
    app.MapHealthChecks(monitoringOptions.HealthEndpoint);
    app.MapControllers();

    return app;
  }
}
