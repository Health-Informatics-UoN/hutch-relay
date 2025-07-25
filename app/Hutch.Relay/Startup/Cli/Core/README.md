# CLI Core

Herein are some classes and suggested usage for:

- Using `System.CommandLine` 2.x (beta5+) with the .NET Generic Host model
  - In particular supporting the use of Generic Host for Configuration and Dependency Injection.
- Only building the Generic Host when invoking actions that need it
- Supporting a Web App with a CLI by running an ASP.NET Core entrypoint as a CLI command (typically the default)

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

Why do we like Generic Host, even in a basic CLI?

Primarily:

- Dependency Injection (that works the same way as ASP.NET Core, which we're used to)
- Configuration loading (that works the same way as ASP.NET Core, which we're used to)

DI is the big one (and is amplified further when adding a CLI to a Web App) as it facilitates easy reuse of existing services without having to manually fulfill dependencies like `ILogger` or Entity Framework `DbContext`s.

It allows your CLI app's bootstrapping to work roughly like most ASP.NET Core tutorials.

### How?

To achieve this we need to:

1. Bootstrap a Generic Host
   - optionally allowing host configuration to depend on `RootCommand` and the CLI `ParseResult`
1. Allow Service registration (`ConfigureServices` style)
1. Have a mechanism for resolving an `Action` at Invoke time from the DI container, instead of constructed by `new()` directly in the `Command`

However, those steps don't necessarily happen in that order, and it can get a bit "chicken and egg".

### Bootstrap a Generic Host

There are so many ways to slice this problem, and `System.CommandLine` provides no opinionated solution today.

You could:

- Build a Host manually inside every Action you write
  - How do you easily share the configuration if desirable (or conditionally not)?
- Build a single Host for all dependent CLI actions up front
- Reuse the same host as other parts of your application (e.g. if you are using WebApplicationBuilder)
  - Should every CLI command register MVC Controllers and Views?
- Dynamically build a CLI-specific Host only when invoking commands that need it.
  - Including sharing the host between multiple actions when that occurs (but giving them independent scopes)
  - Still supporting action-specific Host bootstrapping
  - Still supporting actions that don't use Hosts at all

The CLI Core code allows all of the above, (though it doesn't really help solve anything for the first one).

Mainly it aims at the last one, but if you wanted to take a different approach, it might still help.

Whatever you do, it's worth considering you _usually_ want to bootstrap the Host _after_ you parse the CLI, either because Host configuration depends on CLI arguments, or because whether or not you do it depends on the Parse Result.

It's also worth considering that bootstrapping a .NET Host usually requires `args`, but these are hard to make available later than `Program.cs`.

For this reason, `CliApplication` provides methods focused on using a Factory to create the Host at a later time, accepting customisation methods from the application code to define how the Host is built (after some defaults).

You'll see them in practice later.

### DI Service Registration

Once you have a Host Builder, this can be done as normal.

If you ahve chosen to build the Host yourself, go ahead and do this as you normally would!

If you are using `CliApplication` either to help create the Host, or to Invoke the `ParseResult`, then you can provide a delegate implementation to customise Host creation, including performing service registration as you normally would.

You can also define an extension method on `HostApplicationBuilder` e.g. `ConfigureServices()` that takes and returns the builder when it's done (i.e. using the builder pattern), so you can separate your service configuration from polluting the entrypoint.

This is our recommended style in ASP.NET Core apps too.

### Resolve Actions from the Host

This is the hard bit, especially if you are opting for the most complex last approach in bootstrapping the Host.

`CliApplication` achieves this via a combination of two methods.

First, it uses the Factory pattern to let you specify HOW to build a Host, and then defers that Host creation until Host dependent `Action`s need Invoking.

Host dependent `Action`s are identified with an interface `IDeferredCommandLineAction`.

Secondly, it allows `Command`s to defer their `Action` instantiation by using a `DeferredAction<T>` type, which allows specifying the CLR Type of the `Action` you would like to Invoke when the time comes.

Finally, to bring it all together, it registers event handlers that execute when a `DeferredAction` is invoked.

This is all done via custom `Invoke` methods on `CliApplication`, that wrap the `System.CommandLine`'s `ParseResult` `Invoke` methods.

### `CliApplication.Invoke` Usage

Here's a summary of the simplest usage approach (what you would do to get the behaviour described above)

1. Define `Action`s as classes derived from `SynchronousCommandLineAction` or `AsynchronousCommandLineAction` as appropriate.
   - this is instead of the inline `SetAction(parseResult => ...)` style which works for simple command actions only.
1. Change `Command`s to derive from `DeferredCommand<T>` or `DeferredAsyncCommand<T>` as appropriate, where `T` is the type of the `Action` your `Command` should have.
1. Register your `Action` classes as services wherever you define Host building (e.g. in a factory method)
1. Define a `RootCommand` as normal; optionally as a derived class
1. Use `CliApplication.Invoke()` or `CliApplication.InvokeAsync()` as appropriate.
   - Overloads of these will do the parsing and host building for you <3

Here's what all that might look like:

```csharp
// CLI Entrypoint
var root = new CliRootCommand(args);

// Run the app, with instructions on how to create a CLI Host if necessary
return await CliApplication.InvokeAsync(args, root,

  // We've defined a custom static Host Factory method
  CliHostFactory.Create,

  // We can also override the Host's environment when it's built based on a custom cli option
  parseResult => parseResult.GetValue<string>("--environment"));

// RootCommand
public class CliRootCommand : RootCommand
{
  public CliRootCommand() : base("My App")
  {
    // Here's the option referenced above
    Options.Add(new Option<string?>("--environment", "--environment") { Recursive = true });

    Subcommands.Add(new MyCommand("command"));
  }
}

// SubCommand
public class MyCommand(string name) : DeferredCommand<MyCommandAction>(name, "description")
{
  // Action is set automatically by DeferredCommand<T>, and will resolve MyCommandAction via DI from the Host when Invocation occurs
}

// SubCommand's Action
public class MyCommandAction(SomeDependency dependency) : SynchronousCommandLineAction
{
  public override int Invoke(ParseResult parseResult) { /*...*/ }
}
```

If that works for you, use it.

If you want to get into the weeds of how it works, read on.

- `DeferredCommand<T>` (and its async version `DeferredAsyncCommand<T>`) simply derives from `Command` but automatically sets the `Action` to a special wrapper action which will ask `CliApplication` to raise an event indicating the `DeferredAction` has been invoked.
  - The wrapper functions are `DeferredSynchronousCommandLineAction<T>` and `DeferredAsynchronousCommandLineAction<T>` respectively, and you can use them directly yourself as on any `Command` or `Option`.
- `CliApplication.Invoke` registers handlers for its own events that are triggered when a `DeferredAction` is invoked.
  - The handlers are responsible for building the Host (using the supplied custom factory method) if it has not already been built, and then resolving the `Action` specified by `T` from the host (and therefore all its dependencies). Then it invokes the `Action` `T`.
  - In future allowing custom handlers to be set may be considered. Currently if you wanted this you'd essentially have to implement your own custom `Invoke` functions. `CliApplication`'s could serve as a reference.

## CLI Entrypoint examples

With all of the above, you can combine Generic Host and `System.CommandLine` to produce apps that feel more like modern .NET Web or Console apps.

### Single Generic Host

Here's an example `Program.cs` where we've decided to always create a single shared Generic Host up front as normal, and then use `CliApplication` to allow us to invoke `Command`s with Dependency Injection.

If you're prepared to build the host yourself, and have it always built, this is the simplest scenario.

```csharp
// Program.cs

// Inline RootCommand definition; can be done in a class
RootCommand root = new("My CLI App") {
  SubCommands = {
    new DeferredCommand<MyAction>("command")
  }
};

// Parse the CLI args against RootCommand
var parseResult = root.Parse(args);

// Build the Host
var builder = HostApplicationBuilder.Create();

// Register Deferred Action Targets so they can get their dependencies
builder.Services.AddTransient<MyAction>();

var host = builder.Build();

// Invoke using CliApplication so that DeferredActions work correctly
// In this scenario CliApplication does not do parsing or bootstrap the host as we provide it directly
CliApplication.Invoke(parseResult, host);
```

Some parts should look similar to a Generic Host or even ASP.NET Core app - particularly creating and using the HostBuilder.

### Multi entrypoint example

What if you had an ASP.NET Core app that you wanted to add CLI commands to? So if no commands (i.e. just the root command) were specified, the webapp ran, with all options passed along, but if a defined Subcommand was matched, that would run instead?

In this scenario it's desirable to only create the Generic Host when one or more DeferredActions will be invoked; other commands might not need a host at all, or in the case of the Web App, it will build a WebApplication Host instead, with different configuration!

Here's an example:

```csharp
var root = new CliRootCommand(args);

// Set default CLI action to run the Web App
// If CliRootCommand is a class, you could do this bit in the class (taking `args` in the constructor)
// But having the default action appear in `Program.cs` may be clearer.
root.SetAction((_, ct) => WebEntrypoint.Run(args, ct));

// Run the app, with instructions on how to create a CLI Host if necessary
return await CliApplication.InvokeAsync(args, root,
  CliHostFactory.Create, // Static Factory method for creating a host
  CliRootCommand.Environment); // Static `Option<string?>` on CliRootCommand can be referenced directly

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

// CliHostFactory
public static class CliHostFactory
{

  public static IHost Create(HostApplicationBuilder b, ParseResult parseResult)
  {
    // Configure DI Services
    b.ConfigureServices(parseResult);

    // Build the app
    var host = b.Build();

    // Perform additional initialisation before we run the CLI
    host.Initialise();

    // Invoke the parsed CLI command
    return b.Build();
  }
}
```

See how the `WebEntrypoint` looks mostly like `Program.cs` from an ASP.NET Core app (albeit with some opinionated changes like `ConfigureServices`)?

Also see how `WebEntrypoint` and `CliHostFactory` follow a very similar structure?

They are both really just configuring a Host - though in the `WebEntrypoint` you run it and in `CliHostFactory` you simply return it.

Thanks for coming to my TED Talk.
