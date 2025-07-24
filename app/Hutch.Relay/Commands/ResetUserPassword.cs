using System.CommandLine;
using System.CommandLine.Invocation;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Helpers;
using Hutch.Relay.Startup.Cli.Core;
using Microsoft.AspNetCore.Identity;
using Spectre.Console;

namespace Hutch.Relay.Commands;

internal class ResetUserPassword : HostedAsyncCommand<ResetUserPasswordAction>
{
  public static readonly Argument<string> Username =
    new("username")
    {
      Description = "The user to create a new password for."
    };

  public ResetUserPassword(string name)
    : base(name, "Reset a User's password.")
  {
    Arguments.Add(Username);
  }
}

public class ResetUserPasswordAction(
  [FromKeyedServices("stdout")] IAnsiConsole stdout,
  [FromKeyedServices("stderr")] IAnsiConsole stderr,
  UserManager<RelayUser> users)
  : AsynchronousCommandLineAction
{
  public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
  {
    var username = parseResult.GetRequiredValue(ResetUserPassword.Username);

    var rule = new Rule("[green]Reset User Password[/]")
    {
      Justification = Justify.Left
    };
    stderr.Write(rule);


    var user = await users.FindByNameAsync(username);

    if (user is null)
    {
      stderr.MarkupLine($"[red]:warning: User not found with username {username}.[/]");

      return 1;
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

      return 1;
    }

    var table = new Table { Border = TableBorder.Rounded };

    table.AddColumn("[blue]Username[/]");
    table.AddColumn("[blue]Password[/]");

    table.AddRow(username, password);

    stdout.Write(table);

    return 0;
  }
}
