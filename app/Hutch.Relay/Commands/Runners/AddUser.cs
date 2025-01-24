using System.CommandLine;
using ConsoleTableExt;
using Microsoft.AspNetCore.Identity;
using Hutch.Relay.Commands.Helpers;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Models;
using Hutch.Relay.Services;

namespace Hutch.Relay.Commands.Runners;

public class AddUser(ILoggerFactory logger, IConsole console, UserManager<RelayUser> users, SubNodeService subNodes)
{
  private readonly ILogger<AddUser> _logger = logger.CreateLogger<AddUser>();
  public async Task Run(string username)
  {
    var user = new RelayUser()
    {
      UserName = username
    };
    var password = GeneratePassword.GenerateUniquePassword(16);
    var result = await users.CreateAsync(user, password);
    if (!result.Succeeded)
    {
      _logger.LogInformation("User creation failed with errors for {username}", username);

      var errorRows = new List<List<object>>();

      foreach (var e in result.Errors)
      {
        _logger.LogError(e.Description);
        errorRows.Add(new() { e.Description });
      }

      console.Out.Write(ConsoleTableBuilder
        .From(errorRows)
        .WithCharMapDefinition(CharMapDefinition.FramePipDefinition)
        .Export()
        .ToString());

      return;
    }
    await subNodes.Create(user);
    var outputRows = new List<List<object>>
    {
      new() { "Username", username, "Password", password },
    };

    console.Out.Write(ConsoleTableBuilder
      .From(outputRows)
      .WithCharMapDefinition(CharMapDefinition.FramePipDefinition)
      .Export()
      .ToString());
  }
}
