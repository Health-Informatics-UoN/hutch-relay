using System.CommandLine;

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
