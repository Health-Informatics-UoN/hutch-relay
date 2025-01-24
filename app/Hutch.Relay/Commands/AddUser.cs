using System.CommandLine;
using Hutch.Relay.Commands.Helpers;
using Hutch.Relay.Constants;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services;
using Microsoft.EntityFrameworkCore;

namespace Hutch.Relay.Commands;

internal class AddUser : Command
{
  public AddUser(string name)
    : base(name, "Add a new User.")
  {
    var argUserName = new Argument<string>("username", "The new user name.");
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
              .AddIdentityCore<RelayUser>(DefaultIdentityOptions.Configure)
              .AddEntityFrameworkStores<ApplicationDbContext>();
            s.AddTransient<SubNodeService>();
            s.AddTransient<Runners.AddUser>();
          })
          .GetRequiredService<Runners.AddUser>()
          .Run(username);
      },
      Bind.FromServiceProvider<ILoggerFactory>(),
      Bind.FromServiceProvider<IConfiguration>(),
      Bind.FromServiceProvider<IConsole>(),
      argUserName);
  }
}
