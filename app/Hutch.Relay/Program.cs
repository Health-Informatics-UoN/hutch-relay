using System.CommandLine;
using Hutch.Relay.Startup.Web;
using Serilog;
using Hutch.Relay.Commands;
using Hutch.Relay.Startup;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
  // Display the Logo and version information
  StartupLogo.Display();

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
        new ListUsers("list"),
        new AddUser("add"),
        new ResetUserPassword("reset-password"),
        new AddUserSubNode("add-subnode"),
        new ListUserSubNodes("list-subnodes"),
        new RemoveUserSubNodes("remove-subnodes")
      },

      new("database", "Local Datastore Management actions")
      {
        new DatabaseUpdate("update")
      }
    }
  };

  // Default action to run the Web App
  root.SetAction((_, ct) => WebEntrypoint.Run(args, ct));

  // Parse the command line args
  var parseResult = root.Parse(args);

  // Invoke actions based on the parse result
  return await parseResult.InvokeAsync();
  
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
