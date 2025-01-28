using System.CommandLine;
using ConsoleTableExt;
using Microsoft.AspNetCore.Identity;
using Hutch.Relay.Commands.Helpers;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Models;
using Hutch.Relay.Services;

namespace Hutch.Relay.Commands.Runners;

public class ResetUserPassword(ILoggerFactory logger, IConsole console, UserManager<RelayUser> users)
{
  private readonly ILogger<AddUser> _logger = logger.CreateLogger<AddUser>();

  public async Task Run(string username)
  {
    var user = await users.FindByNameAsync(username);

    if (user is null)
    {
      var errorRows = new List<List<object>>();
      errorRows.Add([$"\u26a0\ufe0f User not found with {username}."]);

      console.Out.Write(ConsoleTableBuilder
        .From(errorRows)
        .WithCharMapDefinition(CharMapDefinition.FramePipDefinition)
        .Export()
        .ToString());

      return;
    }

    var password = GeneratePassword.GenerateUniquePassword(16);

    var resetToken = await users.GeneratePasswordResetTokenAsync(user);

    var result = await users.ResetPasswordAsync(user, resetToken, password);
    if (!result.Succeeded)
    {
      _logger.LogInformation("User password reset failed with errors for {username}", username);

      var errorRows = new List<List<object>>();

      foreach (var e in result.Errors)
      {
        _logger.LogError(e.Description);
        errorRows.Add([e.Description]);
      }

      console.Out.Write(ConsoleTableBuilder
        .From(errorRows)
        .WithCharMapDefinition(CharMapDefinition.FramePipDefinition)
        .Export()
        .ToString());

      return;
    }

    var outputRows = new List<List<object>>
    {
      new() { "Username", "Password" },
      new() { username, password },
    };

    console.Out.Write(ConsoleTableBuilder
      .From(outputRows)
      .WithCharMapDefinition(CharMapDefinition.FramePipDefinition)
      .Export()
      .ToString());
  }
}
