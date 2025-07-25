using System.CommandLine;
using Hutch.Relay.Commands;

namespace Hutch.Relay.Startup.Cli;

public class CliRootCommand : RootCommand
{
  public static Option<string?> Environment { get; } =
    new("--environment", "--environment", "-e")
    {
      Description = "Override the application host's Environment Name.",
      Recursive = true
    };

  public static Option<string?> ConnectionString { get; } =
    new("--connection-string", "--connection-string")
    {
      Description = "Override the local datastore connection string.",
      Recursive = true
    };

  public CliRootCommand(string[] args) : base("Hutch Relay")
  {
    TreatUnmatchedTokensAsErrors = false;

    Options.Add(Environment);
    Options.Add(ConnectionString);

    Subcommands.Add(new("users", "Relay User actions")
      {
        new ListUsers("list"),
        new AddUser("add"),
        new ResetUserPassword("reset-password"),
        new AddUserSubNode("add-subnode"),
        new ListUserSubNodes("list-subnodes"),
        new RemoveUserSubNodes("remove-subnodes")
      });

    Subcommands.Add(new("database", "Local Datastore Management actions")
      {
        new DatabaseUpdate("update")
      });
  }
}

