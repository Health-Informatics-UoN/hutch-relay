using System.CommandLine;
using System.CommandLine.Invocation;

namespace Hutch.Relay.Startup.Cli.Core;

public class HostedSynchronousCommandLineAction<TRootCommand, TAction>() : SynchronousCommandLineAction
  where TAction : SynchronousCommandLineAction
  where TRootCommand : RootCommand, IHostedRootCommand
{

  public override int Invoke(ParseResult parseResult)
  {
    // retrieve the CLI Host from the Root Command
    var cliHost = ((IHostedRootCommand)parseResult.RootCommandResult.Command).CliHost;

    using var scope = (cliHost?.Services.CreateScope())
      ?? throw new InvalidOperationException(
        "CLI Host is not set. Please set the CLI Host before invoking the action.");

    var action = scope.ServiceProvider.GetRequiredService<TAction>();

    return action.Invoke(parseResult);
  }
}
