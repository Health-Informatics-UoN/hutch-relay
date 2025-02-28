using System.CommandLine;
using Hutch.Relay.Commands;

namespace Hutch.Relay.Startup.Cli;

public class CliRootCommand : RootCommand
{
  public CliRootCommand() : base("Hutch Relay")
  {
    AddGlobalOption(new Option<string>(new[] { "--environment", "-e" }));
    
    // Add Commands here
    AddCommand(new("users", "Relay User actions")
    {
      new ListUsers("list"),
      new AddUser("add"),
      new ResetUserPassword("reset-password"),
      new AddUserSubNode("add-subnode")
    });    
    
    AddCommand(new("ef", "Run EF database")
    {
      new RunEfDatabase("database")
    });
  }
}
