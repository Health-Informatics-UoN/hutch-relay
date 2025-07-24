using System.CommandLine;
using Hutch.Relay.Commands;
using Hutch.Relay.Startup.Cli.Core;

namespace Hutch.Relay.Startup.Cli;

public class CliRootCommand : RootCommand
{
  public readonly HostFactory DefaultHostFactory;

  public Option<string?> Environment = new("--environment", "--environment", "-e")
  {
    Description = "Override the application host's Environment Name.",
    Recursive = true
  };

  public Option<string?> ConnectionString = new("--connection-string", "--connection-string")
  {
    Description = "Override the local datastore connection string.",
    Recursive = true
  };

  public CliRootCommand(string[] args) : base("Hutch Relay")
  {
    // Define a Default CliHost Factory that we'll use for most Hosted Commands
    DefaultHostFactory = CliApplication.CreateFactory(args,
      CliHostFactory.Build,
      x => x.GetValue<string>("--environment"));

    TreatUnmatchedTokensAsErrors = false;

    Options.Add(Environment);
    Options.Add(ConnectionString);

    Subcommands.Add(new("users", "Relay User actions")
      {
        new ListUsers("list", DefaultHostFactory),
        new AddUser("add", DefaultHostFactory),
        new ResetUserPassword("reset-password", DefaultHostFactory),
        new AddUserSubNode("add-subnode", DefaultHostFactory),
        new ListUserSubNodes("list-subnodes", DefaultHostFactory),
        new RemoveUserSubNodes("remove-subnodes", DefaultHostFactory)
      });

    Subcommands.Add(new("database", "Local Datastore Management actions")
      {
        new DatabaseUpdate("update", DefaultHostFactory)
      });
  }
}

