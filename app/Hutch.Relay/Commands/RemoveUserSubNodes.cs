using System.CommandLine;
using Hutch.Relay.Commands.Helpers;

namespace Hutch.Relay.Commands;

internal class RemoveUserSubNodes : Command
{
  public RemoveUserSubNodes(string name)
    : base(name, "Remove one or more Sub Nodes belonging to a User.")
  {
    var argUserName = new Argument<string>("username", "The User to remove Sub Nodes for.");
    Add(argUserName);

    var argSubNodes = new Argument<List<string>>("subnode-ids", "The SubNode IDs to remove.");
    Add(argSubNodes);

    var optEmptyUserAction = new Option<string>("--empty-user",
      "Specify what to do with a user if they have no sub nodes left after removal.")
      .FromAmong("keep", "remove");
    Add(optEmptyUserAction);

    var optAutoConfirm = new Option<bool>("--yes", "Automatically say yes to the final confirmation check.");
    optAutoConfirm.AddAlias("-y");
    Add(optAutoConfirm);

    var optRemoveAll = new Option<bool>("--all", "Remove all this user's Sub Nodes.");
    Add(optRemoveAll);

    this.SetHandler(
      async (
        scopeFactory,
        username, subNodeIds,
        emptyUserAction, autoConfirm, removeAll) =>
      {
        using var scope = scopeFactory.CreateScope();

        var runner = scope.ServiceProvider.GetRequiredService<Runners.RemoveUserSubNodes>();

        await runner.Run(username, subNodeIds, emptyUserAction, autoConfirm, removeAll);
      },
      Bind.FromServiceProvider<IServiceScopeFactory>(),
      argUserName, argSubNodes,
      optEmptyUserAction, optAutoConfirm, optRemoveAll);
  }
}
