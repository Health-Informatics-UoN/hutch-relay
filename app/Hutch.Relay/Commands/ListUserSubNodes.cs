using System.CommandLine;
using System.CommandLine.Invocation;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services.Contracts;
using Hutch.Relay.Startup.Cli.Core;
using Microsoft.AspNetCore.Identity;
using Spectre.Console;

namespace Hutch.Relay.Commands;

internal class ListUserSubNodes : HostedAsyncCommand<ListUserSubNodesAction>
{
  public static readonly Argument<string> Username =
    new("username")
    {
      Description = "The User to list Sub Nodes for."
    };

  public ListUserSubNodes(string name)
    : base(name, "List all Sub Nodes for a User.")
  {
    Arguments.Add(Username);
  }
}

public class ListUserSubNodesAction(
  [FromKeyedServices("stdout")] IAnsiConsole stdout,
  [FromKeyedServices("stderr")] IAnsiConsole stderr,
  UserManager<RelayUser> users,
  ISubNodeService subNodes)
  : AsynchronousCommandLineAction
{
  public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
  {
    var username = parseResult.GetRequiredValue(ListUserSubNodes.Username);

    stderr.Write(new Rule("[green]List Collection SubNodes for a User[/]")
    {
      Justification = Justify.Left
    });

    var user = await users.FindByNameAsync(username);

    if (user is null)
    {
      stderr.MarkupLine($"[red]:warning: User could not be found with the username {username}[/]");
      return 1;
    }

    var userSubNodes = (await subNodes.List(username)).Select(x => x.Id.ToString()).ToList();

    if (userSubNodes.Count == 0)
      stderr.MarkupLine($"[red]:warning: No SubNodes found for the user {username}.[/]");
    else
      stdout.Write(
        new Panel(string.Join("\n", userSubNodes))
        {
          Border = BoxBorder.Rounded,
          Header = new($"[blue]SubNodes for User:[/] [green]{username}[/]")
        });

    return 0;
  }
}
