using System.CommandLine;

namespace Hutch.Relay.Startup.Cli.Core;

public abstract class HostedRootCommand(string description) : RootCommand(description), IHostedRootCommand
{
  /// <summary>
  /// The CLI Host that this Root Command is associated with.
  /// This property is set when the CLI Host is built and allows the command to access services and configurations.
  /// </summary>
  public IHost? CliHost { get; set; }
}
