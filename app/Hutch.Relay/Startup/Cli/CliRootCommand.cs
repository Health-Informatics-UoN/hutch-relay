using System.CommandLine;
using Hutch.Relay.Commands;
using Hutch.Relay.Startup.Cli.Core;

namespace Hutch.Relay.Startup.Cli;

public class CliRootCommand : HostedRootCommand
{
  public static readonly Option<string?> OptEnvironment =
    new("--environment", "--environment", "-e")
    {
      Description = "Override the application host's Environment Name.",
      Recursive = true
    };

  public static readonly Option<string?> OptConnectionString =
    new("--connection-string", "--connection-string")
    {
      Description = "Override the local datastore connection string.",
      Recursive = true
    };

  public CliRootCommand() : base("Hutch Relay")
  {
    Options.Add(OptEnvironment);
    Options.Add(OptConnectionString);

    // // Add Subcommands here
    Subcommands.Add(new("users", "Relay User actions")
    {
      new ListUsers("list"),
      new AddUser("add"),
      new ResetUserPassword("reset-password"),
      new AddUserSubNode("add-subnode"),
      // new ListUserSubNodes("list-subnodes"),
      // new RemoveUserSubNodes("remove-subnodes")
    });

    Subcommands.Add(new("database", "Local Datastore Management actions")
    {
      new DatabaseUpdate("update")
    });
  }
}
