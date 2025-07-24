# CLI Core

Herein are some classes and suggested usage for:

- Using `System.CommandLine` 2.x (beta5+) with the .NET Generic Host model
    - In particular supporting the use of Generic Host for Configuration and Dependency Injection.
- Having the entrypoint to a `System.CommandLine` app look and feel roughly like ASP.NET or Generic Host apps.
- Having a CLI in an ASP.NET Core App by using multiple entrypoints

This document also serves to supplement the `System.CommandLine` documentation with patterns that seem sensible.

## `System.CommandLine` basics

`System.CommandLine` is the library used internally by the .NET team for the `dotnet` cli, and other command line experiences. Read about it [here](https://learn.microsoft.com/en-us/dotnet/standard/commandline/).

For most details, read the documentation. Here however is a TL;DR for app development:

- Define a command hierarchy, starting with a `RootCommand`, which may (probably will) have SubCommands derived from `Command`.
- All Commands can have Names (mandatory), Aliases, Descriptions, Arguments, Options etc, largely POSIX styled
    - `Option`s can be global (technically recursive - applied to the `Command` they're defined on, and all Subcommands thereafter)
- From these definitions, `System.CommandLine` can parse your app's arguments and resolve a matching `Command`, provide built-in help, etc...
- To use in an app:
    - first run a parsing step against your defined `RootCommand` and its heirarchy
    - then do what you want with the `ParseResult`
        - typically invoking the resolved `Action`
        - but it's valid to make other decisions based on the result of parsing.
- An `Action` is effectively the "Do Something" code that occurs when you invoke a parsing match.
- An `Action` usually belongs to a `Command`, but may belong to an `Option` (e.g. `--help`)
- `Action`s may be Synchronous or Asynchronous, but a CLI app (from the `RootCommand` down) [should not mix and match](https://learn.microsoft.com/en-us/dotnet/standard/commandline/how-to-parse-and-invoke#asynchronous-actions)

That's `System.CommandLine` by itself in a nutshell. The docs provide examples and further explanation.

## Usage with Generic Host

In larger apps, it's useful to be able to to load configuration in an expected way (such as the way other .NET Application Host apps such as ASP.NET Core do).

It may also be useful to configure available services by registering them in a DI Container (such as the .NET Application Host model provides out-the-box) and then resolving CLI Actions via Dependency Injection.

Basically it also makes CLI Apps built in this way behave more like ASP.NET Core and be more usable with tutorials around the web for logging, resolving services etc.

To achieve this we need:

1. Bootstrap a Generic Host, optionally allowing host configuration to depend on `RootCommand` and the CLI `ParseResult`
1. Allow Service registration (`ConfigureServices` style)
1. Have a mechanism for resolving an `Action` at Invoke time from the DI container, instead of constructed by `new()` directly in the `Command`

The classes provided here facilitate the above, typically with some very lightweight derived classes from `System.CommandLine`.

### Bootstrap a Generic Host

We provide the `CliApplication` static class which offers Generic Host builder helpers tailored slightly for CLI use.

`// TODO: support better host configuration overrides such as environment`

You can bootstrap a Generic Host either getting the builder back, or immediately running a Configure Services action and returning the built host directly.

This should look and feel very close to `WebApplication` in ASP.NET Core:

```csharp
// Create a CLI Host Builder and do stuff with it before building
var b = CliApplication.CreateBuilder(args, parseResult);

// Configure DI Services
b.Services.AddTransient<MyService>();
// ...

// Do other stuff

// Build the app
var host = b.Build();
```

```csharp
// Create a CLI Host App in one go
var host = CliApplication.Create(args, parseResult);

// Optionally with DI Services
var host = CliApplication.Create(args, parseResult,
  builder => builder.Services.AddTransient<MyService>());
```

### DI Service Registration

This is done in the normal way. As seen above `CliApplication` allows it in a similar fashion to ASP.NET Core's `WebApplication` directly on `HostApplicationBuilder.Services`.

You can also define an extension method on `HostApplicationBuilder` e.g. `ConfigureServices()` that takes and returns the builder when it's done (i.e. using the builder pattern), so you can separate your service configuration from polluting the entrypoint.

This is our recommended style in ASP.NET Core apps too.

### Resolve Actions from the Host

CLI Core offers several building blocks for this that are documented in detail here for flexibility, however they are composed into derived classes which should make for the simplest usage in most cases.

The simplest version is:

1. Define `Action`s as classes derived from `SynchronousCommandLineAction` or `AsynchronousCommandLineAction` as appropriate.
    - this is instead of the inline `SetAction(parseResult => ...)` style which works for simple command actions only.
1. Change `Command`s to derive from `HostedCommand<T>` or `HostedAsyncCommand<T>` as appropriate, where `T` is the type of the `Action` your `Command` should have.
1. Register your `Action` classes as services in the Host.
1. Provide a `HostFactory` to the `HostedCommand`s where they are initialised.
    - This is a func that will defer building the host until the `Action` is Invoked.
    - It's up to you if you share one across all (or most) `HostedCommand`s, or they can easily specify their own
    - `CliApplication.CreateFactory` is a helper that allows you to extend it with app-specific needs, and override the App environment based on a custom global option

Here's what all that might look like:

```csharp
// CLI Entrypoint
var cli = new CommandLineConfiguration(new CliRootCommand());
var parseResult = cli.Parse(args);

var host = CliApplication.Create(args, parseResult,
  builder => builder.Services
    .AddTransient<SomeDependency>()
    .AddTransient<MyCommandAction>());

host.UseHostedRootCommand(parseResult);

return parseResult.Invoke();

// RootCommand
public class CliRootCommand : HostedRootCommand
{
  public CliRootCommand() : base("My App")
  {
    Subcommands.Add(new MyCommand("command"));
  }
}

// SubCommand
public class MyCommand(string name) : HostedCommand<MyCommandAction>(name, "description")
{
  // Action is set automatically by HostedCommand<T>, and will resolve MyCommandAction via DI from the Host
}

// SubCommand's Action
public class MyCommandAction(SomeDependency dependency) : SynchronousCommandLineAction
{
  public override int Invoke(ParseResult parseResult) { /*...*/ }
}
```

If that works for you, use it.

If you want to get into the weeds of how it works, read on.

- `HostedRootCommand` simply derives from `RootCommand` and implements `IHostedRootCommand`, which just guarantees a property for `CliHost` on the RootCommand, so that we can get at the generic host during invocation (after parsing).
    - ✅ You can implement `IHostedRootCommand` yourself on a custom `RootCommand` and it will all still work
- `HostedCommand<T>` (and its async version `HostedAsyncCommand<T>`) simply derives from `Command` but automatically sets the `Action` to a special wrapper action which will retrieve the Host from the `IHostedRootCommand`, and then resolve the `Action` specified by `T` from the host (and therefore all its dependencies). Then it invokes the `Action` `T`.
    - The wrapper functions are `HostedSynchronousCommandLineAction<T>` and `HostedAsynchronousCommandLineAction<T>` respectively, and you can use them directly yourself as on any `Command` or `Option` provided the `RootCommand` they belong to implements `IHostedRootCommand`.
    - `CliApplication` provides slightly shorter helpers for wrapping an `Action` type in the above:
        - `CliApplication.Action<T>()`
        - `CliApplication.AsyncAction<T>()`
- `CliApplication` provides an `IHost` extension that sets the `CliHost` property on the `RootCommand` of a `ParseResult`
    - Obviously the `RootCommand` must implement `IHostedRootCommand`
    - calling `host.UseHostedRootCommand(parseResult)` is essential to allowing the wrapper actions to get the host and resolve their actions.

## CLI Entrypoint example

With all of the above, you can combine Generic Host and `System.CommandLine` to produce apps that feel more like modern .NET Web or Console apps.

Here's an example `Program.cs` using all of our techniques above:

```csharp
// Parse the command line based on config and our CLI Root Command definition
var cli = new CommandLineConfiguration(new CliRootCommand());
var parseResult = cli.Parse(args);

var b = CliApplication.CreateBuilder(args, parseResult);

// Configure DI Services
b.ConfigureServices(parseResult);

// Any other builder configuration

// Build the app
var host = b.Build();

// Perform additional initialisation before we run the CLI
await host.Initialise();

// Configure the CLI Root Command to allow using the configured Host
// Necessary for Hosted CommandLine Actions to access the CLI Host and its services.
host.UseHostedRootCommand(parseResult);

// Invoke the parsed CLI command
return await parseResult.InvokeAsync();
```

That shouldn't look far off an ASP.NET Core app's `Program.cs`, albeit we've rolled DI registration into a `ConfigureServices` extension method. `Initialise` is similarly an extension method.

### Multi entrypoint example

What if you had an ASP.NET Core app that you wanted to add CLI commands to? So if no commands (i.e. just the root command) were specified, the webapp ran, with all options passed along, but if a defined Subcommand was matched, that would run instead?

Here's an example:

```csharp
// Program.cs
// Parse the command line based on config and our CLI Root Command definition
var cli = new CommandLineConfiguration(new CliRootCommand());
var parseResult = cli.Parse(args);

// Choose entrypoint based on the command parsed
return parseResult switch
{
  ParseResult { CommandResult.Command: RootCommand, Action: null } // Root Command with no action (e.g. option actions like --help, --version)
    => await WebEntrypoint.Run(args), // Run Web Entrypoint

  _ => await CliEntrypoint.Run(args, parseResult)
};

// WebEntrypoint
public static class WebEntrypoint
{
  public static async Task<int> Run(string[] args)
  {
    var b = WebApplication.CreateBuilder(args);

    // Configure DI Services
    b.ConfigureServices();

    // Build the app
    var app = b.Build();

    // Perform additional initialisation before we run the Web App
    await app.Initialise();

    // Configure the HTTP Request Pipeline
    app.UseWebPipeline();

    // Run the app!
    await app.RunAsync();

    return 0;
  }
}

// CliEntrypoint
public static class CliEntrypoint
{

  public static async Task<int> Run(string[] args, ParseResult parseResult)
  {
    var b = CliApplication.CreateBuilder(args, parseResult);

    // Configure DI Services
    b.ConfigureServices(parseResult);

    // Build the app
    var host = b.Build();

    // Perform additional initialisation before we run the CLI
    await host.Initialise();

    // Configure the CLI Root Command to allow using the configured Host
    // Necessary for Hosted CommandLine Actions to access the CLI Host and its services.
    host.UseHostedRootCommand(parseResult);

    // Invoke the parsed CLI command
    return await parseResult.InvokeAsync();
  }
}
```

See how the `WebEntrypoint` looks mostly like `Program.cs` from an ASP.NET Core app (albeit with some opinionated changes like `ConfigureServices`)?

Also see how both entrypoints follow a very similar structure?

Thanks for coming to my TED Talk.
