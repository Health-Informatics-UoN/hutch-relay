using System.CommandLine;
using Hutch.Relay.Commands;

namespace Hutch.Relay.Startup.Cli;

public class CliRootCommand : RootCommand
{
  public static readonly Option<string?> OptEnvironment =
    new(["--environment", "-e"],
      "Override the application host's Environment Name.");

  public static readonly Option<string?> OptConnectionString =
    new(["--connection-string"],
      "Override the local datastore connection string.");

  public CliRootCommand() : base("Hutch Relay")
  {
    AddGlobalOption(OptEnvironment);
    AddGlobalOption(OptConnectionString);

    // Add Commands here
    AddCommand(new("users", "Relay User actions")
    {
      new ListUsers("list"),
      new AddUser("add"),
      new ResetUserPassword("reset-password"),
      new AddUserSubNode("add-subnode"),
      new ListUserSubNodes("list-subnodes"),
      new RemoveUserSubNodes("remove-subnodes")
    });

    AddCommand(new("database", "Local Datastore Management actions")
    {
      new DatabaseUpdate("update")
    });
  }
}
