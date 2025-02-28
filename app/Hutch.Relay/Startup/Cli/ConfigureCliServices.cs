using System.CommandLine.Builder;
using Hutch.Relay.Commands.Runners;
using Hutch.Relay.Constants;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace Hutch.Relay.Startup.Cli;

public static class ConfigureCliServices
{
  public static HostApplicationBuilder ConfigureServices(this HostApplicationBuilder b)
  {
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
      .AddTransient<SubNodeService>();

    // Command Runners
    b.Services
      .AddTransient<AddUserSubNode>()
      .AddTransient<AddUser>()
      .AddTransient<ListUsers>()
      .AddTransient<ResetUserPassword>();

    return b;
  }
}
