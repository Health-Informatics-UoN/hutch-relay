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
    app.UseHttpsRedirection();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    return app;
  }
}
