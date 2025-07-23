using System.CommandLine;

namespace Hutch.Relay.Startup.Cli.Core.Builder;

public static class CliApplication
{
  public static IHost Create(string[] args, ParseResult parseResult, Func<HostApplicationBuilder, HostApplicationBuilder>? configureHost = null)
  {
    var builder = CreateBuilder(args, parseResult);

    builder = configureHost?.Invoke(builder) ?? builder;

    return builder.Build();
  }

  public static HostApplicationBuilder CreateBuilder(string[] args, ParseResult parseResult)
  {
    // TODO: Specific Generic Host configuration for CLI?

    //-- Modify Host Builder creation

    var hostBuilderSettings = new HostApplicationBuilderSettings
    {
      Args = args // pass on the raw cli args
    };

    // Override environment with the global cli option
    var environment = parseResult.CommandResult.GetValue(CliRootCommand.OptEnvironment);
    if (environment is not null)
      hostBuilderSettings.EnvironmentName = environment;

    // We create a generic host to load config and bootstrap stuff "for free"
    var builder = Host.CreateApplicationBuilder(hostBuilderSettings);


    //-- Introduce extra configuration


    // Override connection string with the global cli option
    var connectionString = parseResult.CommandResult.GetValue(CliRootCommand.OptConnectionString);
    if (connectionString is not null)
      builder.Configuration.AddInMemoryCollection([
        new("ConnectionStrings:Default", connectionString)
      ]);

    return builder;
  }

  /// <summary>
  /// If the <see cref="ParseResult" /> contains a Root Command that implements <see cref="IHostedRootCommand"/>,
  /// this method sets the CLI Host on the Root Command.
  /// This allows the Root Command (and subcommands, via <see cref="HostedAsynchronousCommandLineAction{TRootCommand, TAction}"/> to access the CLI Host and its services.
  /// </summary>
  /// <param name="host"></param>
  /// <param name="parseResult"></param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  public static IHost UseHostedRootCommand(this IHost host, ParseResult parseResult)
  {
    // Retrieve the Root Command from the ParseResult
    if (parseResult.RootCommandResult.Command is not IHostedRootCommand rootCommand)
      throw new InvalidOperationException("The Root Command must implement IHostedRootCommand to access the CLI Host.");

    // Set the CLI Host on the Root Command so it can be used in Command Line Actions
    rootCommand.CliHost = host;

    // Return the host for further use if needed
    return host;
  }
}
