using System.CommandLine;
using ConsoleTableExt;
using Microsoft.AspNetCore.Identity;
using Hutch.Relay.Commands.Helpers;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Models;
using Hutch.Relay.Services;
using Hutch.Relay.Startup.Cli;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace Hutch.Relay.Commands.Runners;

public class ListUsers(
  [FromKeyedServices("stdout")] IAnsiConsole stdout,
  [FromKeyedServices("stderr")] IAnsiConsole stderr,
  UserManager<RelayUser> users)
{
  public Task Run()
  {
    var rule = new Rule("[green]List Users[/]")
    {
      Justification = Justify.Left
    };
    stderr.Write(rule);

    var allUsers = users.Users.AsNoTracking()
      .Select(x => x.UserName)
      .ToList();

    if (!allUsers.Any())
      stderr.MarkupLine("[red]:warning: No users found.[/]");
    else
      stdout.Write(
        new Panel(string.Join("\n", allUsers))
        {
          Border = BoxBorder.Rounded,
          Header = new("[blue]Users[/]")
        });

    return Task.CompletedTask;
  }
}
