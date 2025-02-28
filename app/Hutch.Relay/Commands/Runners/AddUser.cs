using System.CommandLine;
using ConsoleTableExt;
using Microsoft.AspNetCore.Identity;
using Hutch.Relay.Commands.Helpers;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Models;
using Hutch.Relay.Services;
using Hutch.Relay.Startup.Cli;
using Spectre.Console;

namespace Hutch.Relay.Commands.Runners;

public class AddUser(
  [FromKeyedServices("stdout")] IAnsiConsole stdout,
  [FromKeyedServices("stderr")] IAnsiConsole stderr,
  UserManager<RelayUser> users,
  SubNodeService subNodes)
{

  public async Task Run(string username)
  {
    stderr.Write(new Rule("[green]Add User[/]")
    {
      Justification = Justify.Left
    });

    var user = new RelayUser()
    {
      UserName = username
    };

    var password = GeneratePassword.GenerateUniquePassword(16);

    var result = await users.CreateAsync(user, password);


    if (!result.Succeeded)
    {
      stderr.MarkupLine($"[red]:warning: User creation failed with errors for {username}.[/]");

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

    var subNode = await subNodes.Create(user);

    var table = new Table { Border = TableBorder.Rounded };

    table.AddColumn("[blue]Username[/]");
    table.AddColumn("[blue]Password[/]");
    table.AddColumn("[blue]Collection ID[/]");

    table.AddRow(username, password, subNode.Id.ToString());

    stdout.Write(table);
  }
}
