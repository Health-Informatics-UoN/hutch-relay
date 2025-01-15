using System.CommandLine;
using Hutch.Relay.Commands;
using AddUser = Hutch.Relay.Commands.AddUser;

namespace Hutch.Relay.Startup.Cli;

public class CliEntrypoint : RootCommand
{
  public CliEntrypoint() : base("Hutch Relay")
  {
    AddGlobalOption(new Option<string>(new[] { "--environment", "-e" }));
    
    // Add Commands here
    AddCommand(new Command("users", "Add Relay Users")
    {
      new AddUser("add")
    });    
    
    AddCommand(new Command("ef", "Run EF database")
    {
      new RunEfDatabase("database")
    });
    
  }
}
