using Serilog;
using Hutch.Relay.Startup;
using Hutch.Relay.Startup.Cli.Core;
using Hutch.Relay.Startup.Cli;
using Hutch.Relay.Startup.Web;

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

  // Run the app, with instructions on how to create a CLI Host if necessary
  return await CliApplication.InvokeAsync(args, root,
    CliHostFactory.Create,
    CliRootCommand.Environment);
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
