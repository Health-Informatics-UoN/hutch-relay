using System.CommandLine;
using System.CommandLine.Invocation;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services.Contracts;
using Hutch.Relay.Startup.Cli.Core;
using Microsoft.AspNetCore.Identity;
using Spectre.Console;

namespace Hutch.Relay.Commands;

internal class AddUserSubNode : DeferredAsyncCommand<AddUserSubNodeAction>
{
  public static readonly Argument<string> Username =
    new("username")
    {
      Description = "The User to create a Sub Node for."
    };

  public static readonly Option<bool> AutoConfirm =
    new("--yes", "-y")
    {
      Description = "Automatically say yes to the final confirmation check."
    };

  public AddUserSubNode(string name)
    : base(name, "Add a new Sub Node for a User.")
  {
    Arguments.Add(Username);

    Options.Add(AutoConfirm);
  }
}

public class AddUserSubNodeAction(
  [FromKeyedServices("stdout")] IAnsiConsole stdout,
  [FromKeyedServices("stderr")] IAnsiConsole stderr,
  UserManager<RelayUser> users,
  ISubNodeService subNodes)
  : AsynchronousCommandLineAction
{
  public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
  {
    var username = parseResult.GetRequiredValue(AddUserSubNode.Username);
    var autoConfirm = parseResult.GetValue(AddUserSubNode.AutoConfirm);

    stderr.Write(new Rule("[green]Add Collection SubNode to User[/]")
    {
      Justification = Justify.Left
    });

    var user = await users.FindByNameAsync(username);

    if (user is null)
    {
      stderr.MarkupLine($"[red]:warning: User could not be found with the username {username}[/]");
      return 1;
    }

    stderr.Write(new Rule("[blue]Summary[/]")
    {
      Justification = Justify.Left
    });

    stderr.MarkupLine($"[blue]Selected user:[/] {username}");
    stderr.MarkupLine("[green]A new subnode will be created for this user.[/]");

    var confirm = autoConfirm ||
                  stderr.Prompt(new ConfirmationPrompt(
                      "Do you want to proceed?")
                  { DefaultValue = false });

    stderr.Write(new Rule("[blue]Results[/]")
    {
      Justification = Justify.Left
    });

    if (!confirm)
    {
      stderr.MarkupLine("[blue]:information: SubNode was not created.[/]");
      return 1;
    }


    var subNode = await subNodes.Create(user);

    var table = new Table { Border = TableBorder.Rounded };

    table.AddColumn("[blue]Username[/]");
    table.AddColumn("[blue]New Collection ID[/]");

    table.AddRow(username, subNode.Id.ToString());

    stdout.Write(table);

    return 0;
  }
}
