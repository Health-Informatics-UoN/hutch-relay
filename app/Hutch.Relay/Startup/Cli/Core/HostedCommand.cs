using System.CommandLine;
using System.CommandLine.Invocation;

namespace Hutch.Relay.Startup.Cli.Core;

class HostedCommand<T> : Command
  where T : SynchronousCommandLineAction
{
  public HostedCommand(string name, string? description = null)
    : base(name, description)
  {
    Action = new HostedSynchronousCommandLineAction<T>();
  }
}

class HostedAsyncCommand<T> : Command
  where T : AsynchronousCommandLineAction
{
  public HostedAsyncCommand(string name, string? description = null)
    : base(name, description)
  {
    Action = new HostedAsynchronousCommandLineAction<T>();
  }
}
