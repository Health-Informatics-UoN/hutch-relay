using System.CommandLine;
using System.CommandLine.Invocation;
using Hutch.Relay.Services;
using Hutch.Relay.Startup.Cli.Core;
using Spectre.Console;

namespace Hutch.Relay.Commands;

internal class DatabaseUpdate(string name, HostFactory hostFactory)
  : HostedAsyncCommand<DatabaseUpdateAction>(
    name,
    hostFactory,
    "Update the database to the latest migration in this build.")
{ }

internal class DatabaseUpdateAction(
  DbManagementService dbManager,
  [FromKeyedServices("stdout")] IAnsiConsole stdout,
  [FromKeyedServices("stderr")] IAnsiConsole stderr)
  : AsynchronousCommandLineAction
{
  public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
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

    return 0;
  }
}
