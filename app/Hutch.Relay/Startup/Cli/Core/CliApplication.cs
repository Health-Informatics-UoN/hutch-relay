using System.CommandLine;
using System.CommandLine.Invocation;

namespace Hutch.Relay.Startup.Cli.Core;

public static class CliApplication
{
  #region Host Creation

  public static IHost Create(string[] args, ParseResult parseResult, Func<HostApplicationBuilder, ParseResult, IHost>? customizer = null, string? environment = null)
  {
    // Create a standard builder first
    var b = CreateBuilder(args, environment);

    // Finish building the host with the provided function
    // or if null just build our default
    return customizer?.Invoke(b, parseResult) ?? b.Build();
  }

  public static IHost Create(string[] args, ParseResult parseResult, string? environment = null)
    => Create(args, parseResult, null, environment);

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

  #endregion

  #region Invocation

  internal static event EventHandler<DeferredActionInvokedEventArgs>? DeferredActionInvoked;
  internal static event AsyncEventHandler<DeferredActionInvokedEventArgs>? DeferredAsyncActionInvoked;

  private static void RegisterDeferredActionHandlers(ParseResult parseResult, IHost? host, Func<ParseResult, IHost>? createHost)
  {
    if (host is null && createHost is null)
      throw new InvalidOperationException(
        "No host or host factory has been provided. CliApplication cannot invoke Deferred Actions on a Host");

    DeferredActionInvoked += (sender, e) =>
    {
      // Create a host if one wasn't provided (or hasn't been created within the scope of this closure)
      host ??= createHost!.Invoke(parseResult); // we already know createHost can't be null is host is

      // Always give an action its own scope, even if reusing a host
      using var scope = host.Services.CreateScope();

      // Resolve the specified Action Type via the Host
      var action = (SynchronousCommandLineAction)scope.ServiceProvider.GetRequiredService(e.ActionType);

      // Invoke the Action
      e.Result = action.Invoke(parseResult);
    };

    DeferredAsyncActionInvoked += async (sender, e) =>
    {
      // Create a host if one wasn't provided (or hasn't been created within the scope of this closure)
      host ??= createHost!.Invoke(parseResult); // we already know createHost can't be null is host is

      // Always give an action its own scope, even if reusing a host
      using var scope = host.Services.CreateScope();

      // Resolve the specified Action Type via the Host
      var action = (AsynchronousCommandLineAction)scope.ServiceProvider.GetRequiredService(e.ActionType);

      // Invoke the Action
      e.Result = await action.InvokeAsync(parseResult);
    };
  }

  public static int InvokeDeferredAction(object sender, Type actionType)
  {
    // Raise the event on behalf of the DeferredAction, since we own it
    var e = new DeferredActionInvokedEventArgs() { ActionType = actionType };
    DeferredActionInvoked?.Invoke(sender, e);

    // capture and return the result
    return e.Result;
  }

  private static int Invoke(ParseResult parseResult, IHost? host, Func<ParseResult, IHost>? createHost)
  {
    if (host is null && createHost is null)
      throw new InvalidOperationException(
        "No host or host factory has been provided. CliApplication cannot invoke Deferred Actions on a Host");

    RegisterDeferredActionHandlers(parseResult, host, createHost);

    // Invoke the ParseResult via the modified CommandAction
    return parseResult.Invoke();
  }

  public static int Invoke(ParseResult parseResult, IHost host)
    => Invoke(parseResult, host, null);

  public static int Invoke(string[] args, RootCommand root, Func<ParseResult, IHost> createHost)
  {
    // Parse the CLI Root Command
    var parseResult = root.Parse(args);

    return Invoke(parseResult, null, createHost);
  }

  public static int Invoke(
    string[] args,
    RootCommand root,
    Func<HostApplicationBuilder, ParseResult, IHost> createHost,
    string? environment)
    => Invoke(args, root,
      parseResult => Create(args, parseResult, createHost, environment));

  public static int Invoke(
    string[] args,
    RootCommand root,
    Func<HostApplicationBuilder, ParseResult, IHost> createHost,
    Option<string?> environment)
    => Invoke(args, root,
      parseResult => Create(args, parseResult, createHost, parseResult.GetValue(environment)));

  public static int Invoke(
    string[] args,
    RootCommand root,
    Func<HostApplicationBuilder, ParseResult, IHost> createHost,
    Func<ParseResult, string?> environmentResolver)
    => Invoke(args, root,
      parseResult => Create(args, parseResult, createHost, environmentResolver.Invoke(parseResult)));


  public static async Task<int> InvokeDeferredAsyncAction(object sender, Type actionType)
  {
    // Raise the event on behalf of the DeferredAction, since we own it
    var e = new DeferredActionInvokedEventArgs() { ActionType = actionType };

    if (DeferredAsyncActionInvoked is not null)
      await DeferredAsyncActionInvoked.Invoke(sender, e);
    else throw new InvalidOperationException("A DeferredAction Handler error occurred!");

    // capture and return the result
    return e.Result;
  }

  private static async Task<int> InvokeAsync(ParseResult parseResult, IHost? host, Func<ParseResult, IHost>? createHost)
  {
    if (host is null && createHost is null)
      throw new InvalidOperationException(
        "No host or host factory has been provided. CliApplication cannot invoke Deferred Actions on a Host");

    RegisterDeferredActionHandlers(parseResult, host, createHost);

    // Invoke the ParseResult via the modified CommandAction
    return await parseResult.InvokeAsync();
  }

  public static async Task<int> InvokeAsync(ParseResult parseResult, IHost host)
    => await InvokeAsync(parseResult, host, null);

  public static async Task<int> InvokeAsync(string[] args, RootCommand root, Func<ParseResult, IHost> createHost)
  {
    // Parse the CLI Root Command
    var parseResult = root.Parse(args);

    // Resolve and Invoke
    return await InvokeAsync(parseResult, null, createHost);
  }

  public static async Task<int> InvokeAsync(
    string[] args,
    RootCommand root,
    Func<HostApplicationBuilder, ParseResult, IHost> createHost,
    Func<ParseResult, string?> environmentResolver)
    => await InvokeAsync(args, root,
      parseResult => Create(args, parseResult, createHost, environmentResolver.Invoke(parseResult)));

  public static async Task<int> InvokeAsync(
    string[] args,
    RootCommand root,
    Func<HostApplicationBuilder, ParseResult, IHost> createHost,
    Option<string?> environment)
    => await InvokeAsync(args, root,
      parseResult => Create(args, parseResult, createHost, parseResult.GetValue(environment)));

  public static async Task<int> InvokeAsync(
    string[] args,
    RootCommand root,
    Func<HostApplicationBuilder, ParseResult, IHost> createHost,
    string? environment)
    => await InvokeAsync(args, root,
      parseResult => Create(args, parseResult, createHost, environment));
      
  #endregion
}
