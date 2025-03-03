using System.CommandLine;
using Hutch.Relay.Commands.Helpers;
using Hutch.Relay.Constants;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hutch.Relay.Commands;

internal class ResetUserPassword : Command
{
  public ResetUserPassword(string name)
    : base(name, "Reset a User's password.")
  {
    var argUserName = new Argument<string>("username", "The user to create a new password for.");
    Add(argUserName);

    this.SetHandler(
      async (
        scopeFactory,
        username) =>
      {
        using var scope = scopeFactory.CreateScope();
        
        var runner = scope.ServiceProvider.GetRequiredService<Runners.ResetUserPassword>();
        
        await runner.Run(username);
      },
      Bind.FromServiceProvider<IServiceScopeFactory>(),
      argUserName);
  }
}
