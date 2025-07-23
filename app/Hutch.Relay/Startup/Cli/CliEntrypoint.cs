using System.CommandLine;
using System.CommandLine.Invocation;
using Hutch.Relay.Commands;
using Hutch.Relay.Startup.Cli.Core;
using Hutch.Relay.Startup.Cli.Core.Builder;

namespace Hutch.Relay.Startup.Cli;

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
    // await host.Initialise();

    // Configure the CLI Root Command to allow using the configured Host
    host.UseHostedRootCommand(parseResult);

    // Invoke the parsed CLI command
    return await parseResult.InvokeAsync();
  }
}
