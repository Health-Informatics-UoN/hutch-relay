using System.CommandLine;
using Hutch.Relay.Commands.Helpers;

namespace Hutch.Relay.Commands;

internal class DatabaseUpdate : Command
{
  public DatabaseUpdate(string name) : base(name, "Update the database to the latest migration in this build.")
  {
    this.SetHandler(
      async (scopeFactory) =>
      {
        using var scope = scopeFactory.CreateScope();

        var runner = scope.ServiceProvider.GetRequiredService<Runners.DatabaseUpdate>();

        await runner.Run();
      },
      Bind.FromServiceProvider<IServiceScopeFactory>());
  }
}
