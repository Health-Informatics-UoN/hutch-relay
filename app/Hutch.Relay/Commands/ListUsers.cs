using System.CommandLine;
using Hutch.Relay.Commands.Helpers;
using Hutch.Relay.Constants;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services;
using Microsoft.EntityFrameworkCore;

namespace Hutch.Relay.Commands;

internal class ListUsers : Command
{
  public ListUsers(string name)
    : base(name, "List Users.")
  {
    this.SetHandler(
      async (
        scopeFactory) =>
      {
        using var scope = scopeFactory.CreateScope();
        
        var runner = scope.ServiceProvider.GetRequiredService<Runners.ListUsers>();
        
        await runner.Run();
      },
      Bind.FromServiceProvider<IServiceScopeFactory>());
  }
}
