using Microsoft.AspNetCore.Identity;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services;
using Spectre.Console;

namespace Hutch.Relay.Commands.Runners;

public class ListUserSubNodes(
  [FromKeyedServices("stdout")] IAnsiConsole stdout,
  [FromKeyedServices("stderr")] IAnsiConsole stderr,
  UserManager<RelayUser> users,
  SubNodeService subNodes)
{
  public async Task Run(string username, bool autoConfirm = false)
  {
    stderr.Write(new Rule("[green]List Collection SubNodes for a User[/]")
    {
      Justification = Justify.Left
    });

    var user = await users.FindByNameAsync(username);

    if (user is null)
    {
      stderr.MarkupLine($"[red]:warning: User could not be found with the username {username}[/]");
      return;
    }

    var userSubNodes = (await subNodes.List(username)).Select(x => x.Id.ToString()).ToList();

    if (!userSubNodes.Any())
      stderr.MarkupLine($"[red]:warning: No SubNodes found for the user {username}.[/]");
    else
      stdout.Write(
        new Panel(string.Join("\n", userSubNodes))
        {
          Border = BoxBorder.Rounded,
          Header = new($"[blue]SubNodes for User:[/] [green]{username}[/]")
        });
  }
}
