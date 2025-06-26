using Hutch.Relay.Data;
using Microsoft.EntityFrameworkCore;

namespace Hutch.Relay.Tests;

public static class FixtureHelpers
{
  public static ApplicationDbContext NewDbContext(string? dbName = null)
  {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseInMemoryDatabase(databaseName: dbName ?? "TestDatabase")
      .EnableDetailedErrors()
      .EnableSensitiveDataLogging()
      .Options;

    return new ApplicationDbContext(options);
  }
}
