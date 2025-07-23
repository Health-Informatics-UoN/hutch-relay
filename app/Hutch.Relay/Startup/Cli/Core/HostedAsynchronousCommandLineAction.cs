using System.CommandLine;
using System.CommandLine.Invocation;

namespace Hutch.Relay.Startup.Cli.Core;


public class HostedAsynchronousCommandLineAction<TRootCommand, TAction>() : AsynchronousCommandLineAction
  where TAction : AsynchronousCommandLineAction
  where TRootCommand : RootCommand, IHostedRootCommand
{

  public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
  {
    // retrieve the CLI Host from the Root Command
    var cliHost = ((IHostedRootCommand)parseResult.RootCommandResult.Command).CliHost;

    using var scope = (cliHost?.Services.CreateScope())
      ?? throw new InvalidOperationException(
        "CLI Host is not set. Please set the CLI Host before invoking the action.");

    var action = scope.ServiceProvider.GetRequiredService<TAction>();

    return await action.InvokeAsync(parseResult, cancellationToken);
  }
}
