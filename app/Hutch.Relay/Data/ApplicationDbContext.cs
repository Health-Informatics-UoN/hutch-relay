using Hutch.Relay.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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
