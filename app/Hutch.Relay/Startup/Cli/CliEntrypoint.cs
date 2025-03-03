using System.CommandLine.Builder;
using Spectre.Console;

namespace Hutch.Relay.Startup.Cli;

public static class CliEntrypoint
{
  public static Task ConfigureHost(HostApplicationBuilder b)
  {
    // Mainly this method is an opportunity to hook in DI Service Registration
    b.ConfigureServices();

    // It also provides an opportunity to modify anything else about the generic host
    // used to run Command Runner services, if desirable.

    // Any initialisation that should run at the point of configuring the host?
    // More likely this would be done globally in Program.cs
    // but this is an opportunity to run stuff before any Command (except Root Bypass)

    return Task.CompletedTask;
  }

  // Custom (App specific) CLI Middleware

  public static CommandLineBuilder UseCliLogo(this CommandLineBuilder cli) =>
    cli.AddMiddleware(async (context, next) =>
    {
      AnsiConsole
        .Create(new() { Out = new AnsiConsoleOutput(Console.Error) })
        .Write(new FigletText("Hutch Relay")
          .LeftJustified()
          .Color(Color.Blue));

      await next(context);
    });
}
