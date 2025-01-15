using System.CommandLine;
using Hutch.Relay.Commands.Helpers;
using Hutch.Relay.Data;
using Microsoft.EntityFrameworkCore;

namespace Hutch.Relay.Commands;

internal class RunEfDatabase : Command
{
  public RunEfDatabase(string name) : base(name, "Run dotnet ef database.")
  {
    var argCommand = new Argument<string>("command", "The EF database command to run.");
    Add(argCommand);

    this.SetHandler(
      (logger, config, console, command) =>
      {
        // Get connection string and configure services
        var connectionString = config.GetConnectionString("Default");

        // Configure DI and run the command handler
        this
          .ConfigureServices(s =>
          {
            s.AddSingleton<ILoggerFactory>(_ => logger)
              .AddSingleton<IConfiguration>(_ => config)
              .AddSingleton<IConsole>(_ => console)
              .AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(connectionString));
            s.AddLogging();
            s.AddTransient<Runners.RunEfDatabase>(); 
          })
          .GetRequiredService<Runners.RunEfDatabase>()
          .Run(command);
      },
      Bind.FromServiceProvider<ILoggerFactory>(),
      Bind.FromServiceProvider<IConfiguration>(),
      Bind.FromServiceProvider<IConsole>(),
      argCommand);
  }
}
