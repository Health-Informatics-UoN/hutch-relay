using System.CommandLine;
using ConsoleTableExt;
using Microsoft.AspNetCore.Identity;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services;
using Hutch.Relay.Startup.Cli;
using Spectre.Console;

namespace Hutch.Relay.Commands.Runners;

public class AddUserSubNode(
  ILoggerFactory logger,
  [FromKeyedServices("stdout")] IAnsiConsole stdout,
  [FromKeyedServices("stderr")] IAnsiConsole stderr,
  UserManager<RelayUser> users,
  SubNodeService subNodes)
{
  private readonly ILogger<AddUserSubNode> _logger = logger.CreateLogger<AddUserSubNode>();

  public async Task Run(string username, bool autoConfirm = false)
  {
    AnsiConsole.Create(new() { Out = new AnsiConsoleOutput(Console.Out) });

    var user = await users.FindByNameAsync(username);
    if (user is null)
    {
      _logger.LogInformation("User could not be found with the username: {Username}", username);
      stderr.Write($"User could not be found with the username: {username}");
      return;
    }

    // TODO: confirm prompt
    var confirm = autoConfirm || stdout.Prompt(new ConfirmationPrompt("Run prompt example?"));

    if (!confirm)
    {
      stdout.Write("SubNode was not created.");
      return;
    }
      
    var subNode = await subNodes.Create(user);
    var outputRows = new List<List<object>>
    {
      new() { username, subNode.Id },
    };

    stdout.Write(ConsoleTableBuilder
      .From(outputRows)
      .WithColumn("Username", "New Collection ID")
      .WithCharMapDefinition(CharMapDefinition.FramePipDefinition)
      .Export()
      .ToString());
  }
}
