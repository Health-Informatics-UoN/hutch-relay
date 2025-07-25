using System.CommandLine;
using System.CommandLine.Invocation;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services.Contracts;
using Hutch.Relay.Startup.Cli.Core;
using Microsoft.AspNetCore.Identity;
using Spectre.Console;

namespace Hutch.Relay.Commands;

internal class RemoveUserSubNodes : DeferredAsyncCommand<RemoveUserSubNodesAction>
{
  public static readonly Argument<string> Username =
    new("username")
    {
      Description = "The User to remove Sub Nodes for."
    };

  public static readonly Argument<List<string>> SubNodes =
    new("subnode-ids")
    {
      Description = "The SubNode IDs to remove.",
      DefaultValueFactory = _ => []
    };

  public static readonly Option<bool> AutoConfirm =
    new("--yes", "-y")
    {
      Description = "Automatically say yes to the final confirmation check."
    };

  public static readonly Option<string> EmptyUserAction =
    new("--empty-user")
    {
      Description = "Specify what to do with a user if they have no sub nodes left after removal.",
    };

  public static readonly Option<bool> RemoveAll =
    new("--all")
    {
      Description = "Remove all this user's Sub Nodes."
    };

  public RemoveUserSubNodes(string name)
    : base(name, "Remove one or more Sub Nodes belonging to a User.")
  {
    Arguments.Add(Username);
    Arguments.Add(SubNodes);

    Options.Add(AutoConfirm);

    EmptyUserAction.AcceptOnlyFromAmong("keep", "remove");
    Options.Add(EmptyUserAction);

    Options.Add(RemoveAll);
  }
}

public class RemoveUserSubNodesAction(
  [FromKeyedServices("stdout")] IAnsiConsole stdout,
  [FromKeyedServices("stderr")] IAnsiConsole stderr,
  UserManager<RelayUser> users,
  ISubNodeService subNodes)
  : AsynchronousCommandLineAction
{
  public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
  {
    var username = parseResult.GetRequiredValue(RemoveUserSubNodes.Username);
    var subNodeIds = parseResult.GetRequiredValue(RemoveUserSubNodes.SubNodes);
    var emptyUserAction = parseResult.GetValue(RemoveUserSubNodes.EmptyUserAction);
    var autoConfirm = parseResult.GetValue(RemoveUserSubNodes.AutoConfirm);
    var removeAll = parseResult.GetValue(RemoveUserSubNodes.RemoveAll);

    stderr.Write(new Rule("[green]Remove Collection SubNodes for a User[/]")
    {
      Justification = Justify.Left
    });

    if (!removeAll && subNodeIds.Count == 0)
    {
      stderr.MarkupLine(
        $"[red]:warning: No SubNodes specified for removal. Please provide at least one ID, or use the [blue]`--all`[/] option.[/]");
      return 1;
    }

    var user = await users.FindByNameAsync(username);

    if (user is null)
    {
      stderr.MarkupLine($"[red]:warning: User could not be found with the username {username}[/]");
      return 1;
    }

    var emptyUser = removeAll;
    var userSubNodes = (await subNodes.List(username)).Select(x => x.Id.ToString()).ToList();

    if (!removeAll)
    {
      if (subNodeIds.Any(x => !userSubNodes.Contains(x)))
      {
        stderr.MarkupLine(
          $"[red]:warning: One or more of the provided SubNode IDs does not belong to the user: [blue]{username}[/].[/]");

        var grid = new Grid();
        grid.AddColumns(2);

        grid.AddRow(
          new Panel(new Rows(subNodeIds.Select(x => new Text(x))))
          { Border = BoxBorder.Rounded, Header = new("Requested SubNodes") },
          new Panel(userSubNodes.Any()
              ? new Rows(userSubNodes.Select(x => new Text(x)))
              : new Markup("[red]This user has no SubNodes.[/]"))
          { Border = BoxBorder.Rounded, Header = new("User SubNodes") });

        stderr.Write(grid);
        return 1;
      }

      if (userSubNodes.Count == subNodeIds.Count) emptyUser = true;
    }

    var deleteUser = false;
    if (emptyUser)
    {
      deleteUser = emptyUserAction is null
        ? stderr.Prompt(new ConfirmationPrompt(
            "The User will have no Sub Nodes after this action. Do you want to remove the user?")
        { DefaultValue = false })
        : emptyUserAction == "remove";
    }

    stderr.Write(new Rule("[blue]Summary[/]")
    {
      Justification = Justify.Left
    });

    stderr.MarkupLine($"[blue]Selected user:[/] {username}");

    if (removeAll)
      stderr.MarkupLine($"[red]ALL SubNodes will be removed[/]");
    else
      stderr.Write(new Panel(
            new Rows(subNodeIds.Select(x => new Text(x))))
      { Border = BoxBorder.Rounded, Header = new("[blue]SubNodes to remove[/]") }
        .Expand());

    if (emptyUser)
      stderr.MarkupLine(deleteUser
        ? "[red]The Empty User will be removed[/]"
        : "[blue]The Empty User will [red]not[/] be removed[/]");

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
      stderr.MarkupLine("[blue]:information: No SubNodes were removed.[/]");
      return 1;
    }

    var targetSubNodeIds = removeAll ? userSubNodes : subNodeIds;
    foreach (var id in targetSubNodeIds)
      await subNodes.Delete(username, id);

    if (deleteUser)
      await users.DeleteAsync(user);

    stdout.Write(
      new Panel(new Rows(targetSubNodeIds.Select(x => new Text(x))))
      {
        Border = BoxBorder.Rounded,
        Header = new($"[red]Removed SubNodes for User:[/] [green]{username}[/]")
      });

    if (emptyUser)
      stderr.MarkupLine(deleteUser
        ? "[blue]:information: The Empty User was [red]removed[/].[/]"
        : "[blue]:information: The Empty User was [red]not[/] removed.[/]");

    return 0;
  }
}
