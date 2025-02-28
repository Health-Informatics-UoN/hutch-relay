using Microsoft.AspNetCore.Identity;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services;
using Spectre.Console;

namespace Hutch.Relay.Commands.Runners;

public class AddUserSubNode(
  [FromKeyedServices("stdout")] IAnsiConsole stdout,
  [FromKeyedServices("stderr")] IAnsiConsole stderr,
  UserManager<RelayUser> users,
  SubNodeService subNodes)
{
  public async Task Run(string username, bool autoConfirm = false)
  {
    stderr.Write(new Rule("[green]Add Collection SubNode to User[/]")
    {
      Justification = Justify.Left
    });

    var user = await users.FindByNameAsync(username);

    if (user is null)
    {
      stderr.MarkupLine($"[red]:warning: User could not be found with the username {username}[/]");
      return;
    }

    stderr.MarkupLine($"[blue]Selected user:[/] {username}");

    var confirm = autoConfirm ||
                  stderr.Prompt(new ConfirmationPrompt(
                      "Do you want to create a new SubNode for the selected user?")
                    { DefaultValue = false });

    if (!confirm)
    {
      stderr.MarkupLine("[blue]:information: SubNode was not created.[/]");
      return;
    }

    var subNode = await subNodes.Create(user);


    var table = new Table { Border = TableBorder.Rounded };

    table.AddColumn("[blue]Username[/]");
    table.AddColumn("[blue]New Collection ID[/]");

    table.AddRow(username, subNode.Id.ToString());

    stdout.Write(table);
  }
}
