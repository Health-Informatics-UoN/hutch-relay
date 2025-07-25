using System.CommandLine;
using System.CommandLine.Invocation;

namespace Hutch.Relay.Startup.Cli.Core;

class DeferredCommand<T> : Command
  where T : SynchronousCommandLineAction
{
  public DeferredCommand(string name, string? description = null)
    : base(name, description)
  {
    Action = new DeferredSynchronousCommandLineAction<T>();
  }
}

class DeferredAsyncCommand<T> : Command
  where T : AsynchronousCommandLineAction
{
  public DeferredAsyncCommand(string name, string? description = null)
    : base(name, description)
  {
    Action = new DeferredAsynchronousCommandLineAction<T>();
  }
}
