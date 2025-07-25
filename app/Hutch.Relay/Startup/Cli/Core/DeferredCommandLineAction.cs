using System.CommandLine;
using System.CommandLine.Invocation;

namespace Hutch.Relay.Startup.Cli.Core;

internal delegate Task AsyncEventHandler<T>(object sender, T e) where T : EventArgs;

internal class DeferredActionInvokedEventArgs : EventArgs
{
  public required Type ActionType { get; init; }

  public int Result { get; set; }
}

public interface IDeferredCommandLineAction { }

public class DeferredSynchronousCommandLineAction<TAction> : SynchronousCommandLineAction, IDeferredCommandLineAction
  where TAction : SynchronousCommandLineAction
{

  public override int Invoke(ParseResult _)
  {
    return CliApplication.InvokeDeferredAction(this, typeof(TAction));
  }
}


public class DeferredAsynchronousCommandLineAction<TAction> : AsynchronousCommandLineAction, IDeferredCommandLineAction
  where TAction : AsynchronousCommandLineAction
{
  public override async Task<int> InvokeAsync(ParseResult _, CancellationToken __ = default)
  {
    return await CliApplication.InvokeDeferredAsyncAction(this, typeof(TAction));
  }
}
