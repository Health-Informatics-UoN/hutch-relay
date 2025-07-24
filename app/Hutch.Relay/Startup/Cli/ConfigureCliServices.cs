using System.CommandLine;
using Hutch.Relay.Commands;
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
  public static HostApplicationBuilder ConfigureServices(this HostApplicationBuilder b, ParseResult parseResult)
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
          Out = new AnsiConsoleOutput(parseResult.Configuration.Output)
        }))
      .AddKeyedSingleton<IAnsiConsole>("stderr",
        (_, __) => AnsiConsole.Create(new()
        {
          Out = new AnsiConsoleOutput(parseResult.Configuration.Error)
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

    // Command Line Actions
    b.Services
      .AddTransient<DatabaseUpdateAction>()
      .AddTransient<ListUsersAction>()
      .AddTransient<AddUserAction>()
      .AddTransient<AddUserSubNodeAction>()
      .AddTransient<RemoveUserSubNodesAction>()
      .AddTransient<ListUserSubNodesAction>()
      .AddTransient<ResetUserPasswordAction>();

    return b;
  }
}
