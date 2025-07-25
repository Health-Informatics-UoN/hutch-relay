using System.CommandLine;
using System.CommandLine.Invocation;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Startup.Cli.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace Hutch.Relay.Commands;

internal class ListUsers(string name)
  : DeferredAsyncCommand<ListUsersAction>(
    name,
    "List users.")
{ }

public class ListUsersAction(
  [FromKeyedServices("stdout")] IAnsiConsole stdout,
  [FromKeyedServices("stderr")] IAnsiConsole stderr,
  UserManager<RelayUser> users)
  : AsynchronousCommandLineAction
{
  public override Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
  {
    var rule = new Rule("[green]List Users[/]")
    {
      Justification = Justify.Left
    };
    stderr.Write(rule);

    var allUsers = users.Users.AsNoTracking()
      .Select(x => x.UserName)
      .ToList();

    if (allUsers.Count == 0)
      stderr.MarkupLine("[red]:warning: No users found.[/]");
    else
      stdout.Write(
        new Panel(string.Join("\n", allUsers))
        {
          Border = BoxBorder.Rounded,
          Header = new("[blue]Users[/]")
        });

    return Task.FromResult(0);
  }
}
