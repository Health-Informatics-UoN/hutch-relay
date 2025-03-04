using Hutch.Relay.Data;
using Microsoft.EntityFrameworkCore;

namespace Hutch.Relay.Services;

/// <summary>
/// Service for managing the database when programmatically needed
/// e.g. EF Core Migrations
/// </summary>
public class DbManagementService(
  ILogger<DbManagementService> logger,
  ApplicationDbContext db)
{
  public async Task UpdateDatabase()
  {
    try
    {
      await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "An error occurred while applying database migrations.");
      throw;
    }
  }
}
