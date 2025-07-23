using System.CommandLine;

namespace Hutch.Relay.Startup.Cli;

public static class CliEntrypoint
{
  public static async Task<int> Run(ParseResult parseResult)
  {
    // TODO: Configure Generic Host for CLI

    // TODO: ConfigureServices

    // TODO: Any CLI specific (but not command specific) Initialisation?

    // Invoke the appropriate action based on the parse result // TODO: How to use Host?
    return await parseResult.InvokeAsync();
  }
}
