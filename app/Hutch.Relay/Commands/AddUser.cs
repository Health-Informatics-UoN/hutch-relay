using System.CommandLine;
using Hutch.Relay.Commands.Helpers;
using Hutch.Relay.Constants;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services;
using Microsoft.EntityFrameworkCore;

namespace Hutch.Relay.Commands;

internal class AddUser : Command
{
  public AddUser(string name)
    : base(name, "Add a new User.")
  {
    var argUserName = new Argument<string>("username", "The new user name.");
    Add(argUserName);
    
    this.SetHandler(
      async (
        scopeFactory,
        username) =>
      {
        using var scope = scopeFactory.CreateScope();
        
        var runner = scope.ServiceProvider.GetRequiredService<Runners.AddUser>();
        
        await runner.Run(username);
      },
      Bind.FromServiceProvider<IServiceScopeFactory>(),
      argUserName);
  }
}
