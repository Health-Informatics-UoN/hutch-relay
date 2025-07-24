using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq.Expressions;

namespace Hutch.Relay.Startup.Cli.Core;

public static class CliApplication
{
  public static HostFactory CreateFactory(string[] args, HostFactoryExtender? buildHost = null, Func<ParseResult, string?>? environment = null)
  {
    return (parseResult) =>
    {
      // Create a standard builder first
      var b = CreateBuilder(args, environment?.Invoke(parseResult));

      // Finish building the host with the provided function
      // or if null just build our default
      return buildHost?.Invoke(b, parseResult) ?? b.Build();
    };
  }

  public static HostApplicationBuilder CreateBuilder(string[] args, string? environment = null)
  {
    var hostBuilderSettings = new HostApplicationBuilderSettings
    {
      Args = args // pass on the raw cli args
    };

    // Optionally override environment BEFORE we create the builder <3
    if (environment is not null) hostBuilderSettings.EnvironmentName = environment;

    // Create a generic host to load config and bootstrap stuff "for free"
    var builder = Host.CreateApplicationBuilder(hostBuilderSettings);

    return builder;
  }

  /// <summary>
  /// Creates a Hosted Asynchronous Command Line Action for the specified <typeparamref name="TAction"/>.
  /// </summary>
  /// <typeparam name="TAction"></typeparam>
  /// <returns></returns>
  public static HostedAsynchronousCommandLineAction<TAction> AsyncAction<TAction>(HostFactory hostFactory)
    where TAction : AsynchronousCommandLineAction
  {
    return new HostedAsynchronousCommandLineAction<TAction>(hostFactory);
  }

  /// <summary>
  /// Creates a Hosted Synchronous Command Line Action for the specified <typeparamref name="TAction"/>.
  /// </summary>
  /// <typeparam name="TAction"></typeparam>
  /// <returns></returns>
  public static HostedSynchronousCommandLineAction<TAction> Action<TAction>(HostFactory hostFactory)
    where TAction : SynchronousCommandLineAction
  {
    return new HostedSynchronousCommandLineAction<TAction>(hostFactory);
  }
}
