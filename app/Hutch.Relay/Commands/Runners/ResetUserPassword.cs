using Microsoft.AspNetCore.Identity;
using Hutch.Relay.Data.Entities;
using Spectre.Console;
using Hutch.Relay.Helpers;

namespace Hutch.Relay.Commands.Runners;

public class ResetUserPassword(
  [FromKeyedServices("stdout")] IAnsiConsole stdout,
  [FromKeyedServices("stderr")] IAnsiConsole stderr,
  UserManager<RelayUser> users)
{
  public async Task Run(string username)
  {
    var rule = new Rule("[green]Reset User Password[/]")
    {
      Justification = Justify.Left
    };
    stderr.Write(rule);
    
    
    var user = await users.FindByNameAsync(username);

    if (user is null)
    {
      stderr.MarkupLine($"[red]:warning: User not found with username {username}.[/]");

      return;
    }

    var password = GeneratePassword.GenerateUniquePassword(16);

    var resetToken = await users.GeneratePasswordResetTokenAsync(user);

    var result = await users.ResetPasswordAsync(user, resetToken, password);
    if (!result.Succeeded)
    {
      stderr.MarkupLine($"[red]:warning: User password reset failed with errors for {username}.[/]");

      var errorRows = result.Errors
        .Select(e => new Text(e.Description, new(Color.Red, Color.Black)))
        .ToList();

      stderr.Write(new Rule("[red]Errors[/]")
      {
        Justification = Justify.Left
      });

      stderr.Write(new Rows(errorRows));

      return;
    }
    
    var table = new Table { Border = TableBorder.Rounded };
    
    table.AddColumn("[blue]Username[/]");
    table.AddColumn("[blue]Password[/]");

    table.AddRow(username, password);

    stdout.Write(table);
  }
}
