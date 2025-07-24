using System.CommandLine;
using Hutch.Relay.Startup.Web;
using Serilog;
using Hutch.Relay.Commands;
using Hutch.Relay.Startup;
using Hutch.Relay.Startup.Cli.Core;
using Hutch.Relay.Startup.Cli;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
  // Display the Logo and version information
  StartupLogo.Display();

  // Define the CLI as a command heirarchy
  var root = new CliRootCommand(args);

  // Set default CLI action to run the Web App
  root.SetAction((_, ct) => WebEntrypoint.Run(args, ct));

  // Run the app
  return await root.Parse(args).InvokeAsync();
}
catch (Exception ex) when (ex.GetType().Name is not "HostAbortedException") // EF Core tooling exception can be ignored
{
  Log.Fatal(ex, "An unhandled exception occurred during bootstrapping");
  return 1;
}
finally
{
  Log.CloseAndFlush();
}
