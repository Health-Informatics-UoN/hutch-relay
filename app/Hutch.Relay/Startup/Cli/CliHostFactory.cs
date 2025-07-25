using System.CommandLine;


// Hosted CLI steps

  // 1. Parse Root (args) -> (this is all System.Commandline)
  // 2. Build Host (args, parseResult?, hostFactory?) -> CliApplication.Create / CliApplication.CreateBuilder / HostFactory
  // 3. Resolve Deferred Actions (host, parseResult) -> manual or (1/2)3/4 with CliApplication.Invoke
  // 4. Invoke ParseResult -> manual or (1/2)3/4 with CliApplication.Invoke

  // Helpers -> x
  // Invoke(args, root, hostFactory) -> Does all steps, allows hostFactory to customise host building
  // Invoke(parseResult, host) -> Does Deferred Action resolution and Invokes the result only, allows prebuilt host to customise hot building (but requires pre-parsing)

  // DeferredAction handling
  // Provide and wire up event handlers for deferred actions
  // CliApplication COULD expose an API for replacing the built-in handlers for experienced users?
  //   Would have to make the default handlers available if you were using Invoke so you oculd revert
  //   But you could use DeferredActions without CliApplication.Invoke if you didn't need the base handlers
  //   Or add a "handled" status to EventArgs so that the base handlers would run if not handled
  //   Usage without Invoke is still weird though since that does the wiring
  // Manual handling could be written to do different things (e.g. bootstrap a different host) for different DeferredAction<T>, depending on the child Action
  //   This deferring would enable easy host creation (e.g. use of args) but not require the same everytime.
  //   I'd expect users to write this themselves, possibly using Invoke() as an example

namespace Hutch.Relay.Startup.Cli;

public static class CliHostFactory
{
  public static IHost Create(HostApplicationBuilder b, ParseResult parseResult)
  {
    // Perform additional Builder configuration

    // Override connection string with the global cli option
    var connectionString = parseResult.GetValue(CliRootCommand.ConnectionString);
    if (connectionString is not null)
      b.Configuration.AddInMemoryCollection([
        new("ConnectionStrings:Default", connectionString)
      ]);

    // Configure DI Services
    b.ConfigureServices(parseResult);

    // Build the host
    var host = b.Build();

    // Perform additional Host configuration

    // Perform additional initialisation
    // await host.Initialise();

    return host;
  }
}
