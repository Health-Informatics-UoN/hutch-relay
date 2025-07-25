using System.CommandLine;
using System.CommandLine.Invocation;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Helpers;
using Hutch.Relay.Services.Contracts;
using Hutch.Relay.Startup.Cli.Core;
using Microsoft.AspNetCore.Identity;
using Spectre.Console;

namespace Hutch.Relay.Commands;

internal class AddUser : DeferredAsyncCommand<AddUserAction>
{
  public static readonly Argument<string> Username =
    new("username")
    {
      Description = "The new user name."
    };

  public AddUser(string name)
    : base(name, "Add a new User.")
  {
    Arguments.Add(Username);
  }
}

internal class AddUserAction(
  [FromKeyedServices("stdout")] IAnsiConsole stdout,
  [FromKeyedServices("stderr")] IAnsiConsole stderr,
  UserManager<RelayUser> users,
  ISubNodeService subNodes)
  : AsynchronousCommandLineAction
{

  public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
  {
    var username = parseResult.GetRequiredValue(AddUser.Username);

    if (string.IsNullOrWhiteSpace(username))
    {
      stderr.MarkupLine("[red]:warning: Username cannot be empty.[/]");
      return 1;
    }

    if (await users.FindByNameAsync(username) is not null)
    {
      stderr.MarkupLine($"[red]:warning: User {username} already exists.[/]");
      return 1;
    }
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

        return 1;
      }

      var subNode = await subNodes.Create(user);

      var table = new Table { Border = TableBorder.Rounded };

      table.AddColumn("[blue]Username[/]");
      table.AddColumn("[blue]Password[/]");
      table.AddColumn("[blue]Collection ID[/]");

      table.AddRow(username, password, subNode.Id.ToString());

      stdout.Write(table);

      return 0;
    }
  }
}
