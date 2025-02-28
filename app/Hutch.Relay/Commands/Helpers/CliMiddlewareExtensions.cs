using System.CommandLine.Builder;
using Hutch.Relay.Startup.Cli;

namespace Hutch.Relay.Commands.Helpers;

public static class CliMiddlewareExtensions
{
  /// <summary>
  /// Adds middleware that will bypass System.CommandLine for the root command
  /// and instead simply pass through `args` and execute the provided Action
  /// </summary>
  /// <param name="cli">The CommandLineBuilder instance</param>
  /// <param name="args">the cli arguments to pass through when overriding the root command</param>
  /// <param name="newRoot">The async Func to execute instead of the root command</param>
  /// <returns></returns>
  public static CommandLineBuilder UseRootCommandBypass(this CommandLineBuilder cli, string[] args,
    Func<string[], Task> newRoot) =>
    cli.AddMiddleware(async (context, next) =>
    {
      if (context.ParseResult.CommandResult.Command == cli.Command)
      {
        await newRoot.Invoke(args);
      }
      // otherwise, carry on as normal
      else await next(context);
    });

  /// <summary>
  /// Configure a Generic Host for the CLI and make its registered services available
  /// </summary>
  /// <param name="cli"></param>
  /// <param name="args"></param>
  /// <returns></returns>
  public static CommandLineBuilder UseCliHost(this CommandLineBuilder cli, string[] args, Func<HostApplicationBuilder, Task> configureHost)
  {
    // We used to build the host in advance, but now that we use it for full DI registration
    // We don't want to build it in the case of RootCommandBypass
    // So instead we build it inside the middleware,
    // only if we reach it in the middleware pipeline,
    // before handling a given Command
    
    return cli.AddMiddleware(async (context, next) =>
    {
      // We create a generic host to load config and bootstrap stuff "for free"
      var builder = Host.CreateApplicationBuilder(args);

      // allow for external configuration to be applied
      // (primarily DI service registration)
      await configureHost.Invoke(builder);

      var host = builder.Build();

      // Then we use the middleware to add scoped access to the host's service catalog,
      // so any command handler can use service location to run a scoped entrypoint service (e.g. a Command Runner)
      // which will in turn trigger dependency injection of any required services via our host
      context.BindingContext.AddService(s =>
        host.Services.GetRequiredService<IServiceScopeFactory>());

      await next(context);
    });
  }
}
