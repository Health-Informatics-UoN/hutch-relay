using System.CommandLine;
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

  // Display the Logo and version information
  CliLogo.Display();

  // Parse the command line based on config and our CLI Root Command definition
  var cli = new CommandLineConfiguration(new CliRootCommand());
  var parseResult = cli.Parse(args);

  // Choose entrypoint based on the command parsed
  return parseResult switch
  {
    ParseResult { CommandResult.Command: RootCommand, Action: null } // Root Command with no action (e.g. option actions like --help, --version)
      => await WebEntrypoint.Run(args), // Run Web Entrypoint

    _ => await CliEntrypoint.Run(args, parseResult)
  };
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
