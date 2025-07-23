using System.CommandLine;
using System.CommandLine.Invocation;

namespace Hutch.Relay.Startup.Cli.Core;

public class HostedSynchronousCommandLineAction<TAction>() : SynchronousCommandLineAction
  where TAction : SynchronousCommandLineAction
{

  public override int Invoke(ParseResult parseResult)
  {
    // retrieve the CLI Host from the Root Command
    var cliHost = ((IHostedRootCommand)parseResult.RootCommandResult.Command).CliHost;

    using var scope = (cliHost?.Services.CreateScope())
      ?? throw new InvalidOperationException(
        "CLI Host is not set. Please ensure your RootCommand implements IHostedRootCommand and the CLI Host is set correctly.");

    var action = scope.ServiceProvider.GetRequiredService<TAction>();

    return action.Invoke(parseResult);
  }
}
