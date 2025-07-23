using System.CommandLine;
using Hutch.Relay.Commands.Helpers;
using Hutch.Relay.Startup.Web;
using Hutch.Relay.Startup.Cli;
using Hutch.Relay.Startup.EfCoreMigrations;
using Serilog;
using System.CommandLine.Parsing;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
  // Enable EF Core tooling to get a DbContext configuration
  EfCoreMigrations.BootstrapDbContext(args);

  // SCL beta4
  // await new CommandLineBuilder(new CliRootCommand())
  //   .UseDefaults()
  //   .UseCliLogo()
  //   .UseRootCommandBypass(args, WebEntrypoint.Run)
  //   .UseCliHost(args, CliEntrypoint.ConfigureHost)
  //   .Build()
  //   .InvokeAsync(args);

  // SCL 2.x

  // Always run the logo/version? I guess this has nothing to do with the CLI now? What if there's a parsing failure? Do we do the logo independently, or add it to the Help Action?
  CliLogo.Display(); // equivalent to UseCliLogo but not bound up in the CommandLineBuilder

  // Parse the command line based on config and our CLI Root Command definition
  var cli = new CommandLineConfiguration(new CliRootCommand()); // equivalent to UseDefaults()
  var parseResult = cli.Parse(args);

  return parseResult switch
  {
    ParseResult { CommandResult.Command: RootCommand, Action: null } // Root Command with no action (e.g. option actions like --help, --version)
      => await WebEntrypoint.Run(args), // Run Web Entrypoint

    _ => await CliEntrypoint.Run(parseResult)
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
