using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Hutch.Relay.Commands.Helpers;
using Hutch.Relay.Startup.Web;
using Hutch.Relay.Startup.Cli;
using Hutch.Relay.Startup.EfCoreMigrations;

// Enable EF Core tooling to get a DbContext configuration
EfCoreMigrations.BootstrapDbContext(args);

await new CommandLineBuilder(new CliRootCommand())
  .UseDefaults()
  .UseCliLogo()
  .UseRootCommandBypass(args, WebEntrypoint.Run)
  .UseCliHost(args, CliEntrypoint.ConfigureHost)
  .Build()
  .InvokeAsync(args);
