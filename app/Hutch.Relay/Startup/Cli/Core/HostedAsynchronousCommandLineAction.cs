using System.CommandLine;
using System.CommandLine.Invocation;

namespace Hutch.Relay.Startup.Cli.Core;


public class HostedAsynchronousCommandLineAction<TAction>(HostFactory hostFactory) : AsynchronousCommandLineAction
  where TAction : AsynchronousCommandLineAction
{

  public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
  {
    // Build a CLI Host
    var host = hostFactory.Invoke(parseResult);

    using var scope = host.Services.CreateScope();

    var action = scope.ServiceProvider.GetRequiredService<TAction>();

    return await action.InvokeAsync(parseResult, cancellationToken);
  }
}
