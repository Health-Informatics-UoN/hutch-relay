using System.Diagnostics;
using Hutch.Relay.Services;
using Spectre.Console;

namespace Hutch.Relay.Commands.Runners;

public class DatabaseUpdate(
  DbManagementService dbManager,
  [FromKeyedServices("stdout")] IAnsiConsole stdout,
  [FromKeyedServices("stderr")] IAnsiConsole stderr
)

{
  public async Task Run()
  {
    var rule = new Rule("[green]Update Database[/]")
    {
      Justification = Justify.Left
    };
    stderr.Write(rule);

    await stderr.Status()
      .StartAsync("Updating Database to the latest migration...",
        async context => { await dbManager.UpdateDatabase(); });

    // If anything goes wrong, the manager will throw
    stdout.MarkupLine("[blue]The Relay Database is up to date![/]");
  }
}
