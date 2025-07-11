using Hutch.Relay.Commands.Runners;
using Hutch.Relay.Constants;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Spectre.Console;

namespace Hutch.Relay.Startup.Cli;

public static class ConfigureCliServices
{
  public static HostApplicationBuilder ConfigureServices(this HostApplicationBuilder b)
  {
    b.Services.AddSerilog((services, lc) => lc
      .ReadFrom.Configuration(b.Configuration)
      .ReadFrom.Services(services)
      .Enrich.FromLogContext());

    // Console output services
    b.Services
      .AddKeyedSingleton<IAnsiConsole>("stdout",
        (_, __) => AnsiConsole.Create(new()
        {
          Out = new AnsiConsoleOutput(Console.Out)
        }))
      .AddKeyedSingleton<IAnsiConsole>("stderr",
        (_, __) => AnsiConsole.Create(new()
        {
          Out = new AnsiConsoleOutput(Console.Error)
        }));

    // DB Context
    b.Services.AddDbContext<ApplicationDbContext>(o =>
      o.UseNpgsql(b.Configuration.GetConnectionString("Default")));

    // Identity
    b.Services.AddDataProtection();
    b.Services
      .AddIdentityCore<RelayUser>(DefaultIdentityOptions.Configure)
      .AddDefaultTokenProviders()
      .AddEntityFrameworkStores<ApplicationDbContext>();

    // Application Services
    b.Services
      .AddTransient<ISubNodeService, SubNodeService>()
      .AddTransient<DbManagementService>();

    // Command Runners
    b.Services
      .AddTransient<AddUserSubNode>()
      .AddTransient<AddUser>()
      .AddTransient<ListUsers>()
      .AddTransient<ListUserSubNodes>()
      .AddTransient<RemoveUserSubNodes>()
      .AddTransient<ResetUserPassword>()
      .AddTransient<DatabaseUpdate>();

    return b;
  }
}
