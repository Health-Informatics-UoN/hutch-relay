// using System.CommandLine;
// using Hutch.Relay.Commands.Helpers;

// namespace Hutch.Relay.Commands;

// internal class AddUserSubNode : Command
// {
//   public AddUserSubNode(string name)
//     : base(name, "Add a new Sub Node for a User.")
//   {
//     var argUserName = new Argument<string>("username", "The User to create a Sub Node for.");
//     Add(argUserName);

//     var optAutoConfirm = new Option<bool>("--yes", "Automatically say yes to the final confirmation check.");
//     optAutoConfirm.AddAlias("-y");
//     Add(optAutoConfirm);

//     this.SetHandler(
//       async (
//         scopeFactory,
//         username,
//         autoConfirm) =>
//       {
//         using var scope = scopeFactory.CreateScope();

//         var runner = scope.ServiceProvider.GetRequiredService<Runners.AddUserSubNode>();

//         await runner.Run(username, autoConfirm);
//       },
//       Bind.FromServiceProvider<IServiceScopeFactory>(),
//       argUserName,
//       optAutoConfirm);
//   }
// }
