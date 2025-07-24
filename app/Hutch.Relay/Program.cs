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

  // Define a shared CliHost Factory that we'll use for most Hosted Commands
  var hostFactory = CliApplication.CreateFactory(args,
    CliHostFactory.Build,
    x => x.GetValue<string>("--environment"));

  // Define the CLI as a command heirarchy
  RootCommand root = new("Hutch Relay")
  {
    TreatUnmatchedTokensAsErrors = false, 

    Options = {
       new Option<string?>("--environment", "--environment", "-e")
      {
        Description = "Override the application host's Environment Name.",
        Recursive = true
      },
      new Option<string?>("--connection-string", "--connection-string")
      {
        Description = "Override the local datastore connection string.",
        Recursive = true
      },
    },

    Subcommands = {
      new("users", "Relay User actions")
      {
        new ListUsers("list", hostFactory),
        new AddUser("add", hostFactory),
        new ResetUserPassword("reset-password", hostFactory),
        new AddUserSubNode("add-subnode", hostFactory),
        new ListUserSubNodes("list-subnodes", hostFactory),
        new RemoveUserSubNodes("remove-subnodes", hostFactory)
      },

      new("database", "Local Datastore Management actions")
      {
        new DatabaseUpdate("update", hostFactory)
      }
    }
  };

  // Default action to run the Web App
  root.SetAction((_, ct) => WebEntrypoint.Run(args, ct));

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
