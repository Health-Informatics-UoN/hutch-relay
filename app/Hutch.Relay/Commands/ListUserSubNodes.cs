// using System.CommandLine;
// using Hutch.Relay.Commands.Helpers;

// namespace Hutch.Relay.Commands;

// internal class ListUserSubNodes : Command
// {
//   public ListUserSubNodes(string name)
//     : base(name, "List all Sub Nodes for a User.")
//   {
//     var argUserName = new Argument<string>("username", "The User to list Sub Nodes for.");
//     Add(argUserName);

//     this.SetHandler(
//       async (
//         scopeFactory,
//         username) =>
//       {
//         using var scope = scopeFactory.CreateScope();

//         var runner = scope.ServiceProvider.GetRequiredService<Runners.ListUserSubNodes>();

//         await runner.Run(username);
//       },
//       Bind.FromServiceProvider<IServiceScopeFactory>(),
//       argUserName);
//   }
// }
