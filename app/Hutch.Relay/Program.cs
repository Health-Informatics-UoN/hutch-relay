using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Hutch.Relay.Commands.Helpers;
using Hutch.Relay.Startup.Web;
using Hutch.Relay.Startup.Cli;
using Hutch.Relay.Startup.EfCoreMigrations;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
  // Enable EF Core tooling to get a DbContext configuration
  EfCoreMigrations.BootstrapDbContext(args);

  await new CommandLineBuilder(new CliRootCommand())
    .UseDefaults()
    .UseCliLogo()
    .UseRootCommandBypass(args, WebEntrypoint.Run)
    .UseCliHost(args, CliEntrypoint.ConfigureHost)
    .Build()
    .InvokeAsync(args);

  return 0;
}
catch (Exception ex)
{
  Log.Fatal(ex, "An unhandled exception occurred during bootstrapping");
  return 1;
}
finally
{
  Log.CloseAndFlush();
}