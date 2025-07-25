using Hutch.Relay.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hutch.Relay.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
  : IdentityDbContext<IdentityUser>(options)
{
  public DbSet<RelayUser> RelayUsers { get; set; }
  public DbSet<SubNode> SubNodes { get; set; }
  public DbSet<RelayTask> RelayTasks { get; set; }
  public DbSet<RelaySubTask> RelaySubTasks { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<RelayTask>()
      .Property(t => t.CreatedAt)
      .HasDefaultValueSql("CURRENT_TIMESTAMP");
  }
}

// When using complex Startup patterns, EF Core tooling cannot always determine how to get a DbContext.
// So we provide one unambiguously here, per https://learn.microsoft.com/en-us/ef/core/cli/dbcontext-creation?tabs=dotnet-core-cli#from-a-design-time-factory
// We happen to bootstrap a Generic Host to achieve it, because it buys us free stuff like Configuration loading :)
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>, IDisposable
{
  private IServiceScope? _scope;

  public ApplicationDbContext CreateDbContext(string[] args)
  {
    // Bootstrap a Generic Host to easily consume config from all normal sources :)
    var host = Host.CreateDefaultBuilder(args)
      .ConfigureServices((b, s) =>
      {
        var connectionString =
          b.Configuration.GetConnectionString("Default");

        s.AddDbContext<ApplicationDbContext>(
            o =>
            {
              // migration bundles don't like null connection strings (yet)
              // https://github.com/dotnet/efcore/issues/26869
              // so if no connection string is set we register without one for now.
              // if running migrations, `--connection` should be set on the command line
              // in real environments, connection string should be set via config
              // all other cases will error when db access is attempted.
              if (string.IsNullOrWhiteSpace(connectionString))
                o.UseNpgsql();
              else
                o.UseNpgsql(connectionString,
                  pgo => pgo.EnableRetryOnFailure());
            });
      })
      .Build();

    _scope = host.Services.CreateScope();

    return _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
  }

  public void Dispose()
  {
    _scope?.Dispose();
  }
}
