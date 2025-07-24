using System.CommandLine;
using System.CommandLine.Invocation;

namespace Hutch.Relay.Startup.Cli.Core;

public class HostedSynchronousCommandLineAction<TAction>(HostFactory hostFactory) : SynchronousCommandLineAction
  where TAction : SynchronousCommandLineAction
{

  public override int Invoke(ParseResult parseResult)
  {
    // Build a CLI Host
    var host = hostFactory.Invoke(parseResult);

    using var scope = host.Services.CreateScope();

    var action = scope.ServiceProvider.GetRequiredService<TAction>();

    return action.Invoke(parseResult);
  }
}
