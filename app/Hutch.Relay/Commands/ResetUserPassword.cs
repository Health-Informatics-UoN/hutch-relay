using System.CommandLine;
using Hutch.Relay.Commands.Helpers;
using Hutch.Relay.Constants;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hutch.Relay.Commands;

internal class ResetUserPassword : Command
{
  public ResetUserPassword(string name)
    : base(name, "Reset a User's password.")
  {
    var argUserName = new Argument<string>("username", "The user to create a new password for.");
    Add(argUserName);

    this.SetHandler(
      async (
        logger, config, console,
        username) =>
      {
        // figure out the connection string from the option, or config
        var connectionString = config.GetConnectionString("Default");

        // Configure DI and run the command handler
        await this
          .ConfigureServices(s =>
          {
            s.AddSingleton<ILoggerFactory>(_ => logger)
              .AddSingleton<IConfiguration>(_ => config)
              .AddSingleton<IConsole>(_ => console)
              .AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(connectionString));
            s.AddLogging()
              .AddDataProtection();
            s.AddIdentityCore<RelayUser>(DefaultIdentityOptions.Configure)
              .AddDefaultTokenProviders()
              .AddEntityFrameworkStores<ApplicationDbContext>();
            s.AddTransient<Runners.ResetUserPassword>();
          })
          .GetRequiredService<Runners.ResetUserPassword>()
          .Run(username);
      },
      Bind.FromServiceProvider<ILoggerFactory>(),
      Bind.FromServiceProvider<IConfiguration>(),
      Bind.FromServiceProvider<IConsole>(),
      argUserName);
  }
}
