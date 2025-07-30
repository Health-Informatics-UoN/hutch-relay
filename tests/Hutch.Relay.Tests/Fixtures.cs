using System.Data.Common;
using Hutch.Relay.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Hutch.Relay.Tests;

public static class FixtureHelpers
{
  public static ApplicationDbContext NewDbContext(ref DbConnection? sqliteConnection)
  {
    // EF Core In-Memory is a) not great and b) not workable for us given features we use
    // https://learn.microsoft.com/en-us/ef/core/testing/testing-without-the-database?source=recommendations#sqlite-in-memory

    // Create and open a connection. This creates the SQLite in-memory database, which will persist until the connection is closed
    // at the end of the test (see Dispose below).
    sqliteConnection = new SqliteConnection("Filename=:memory:");
    sqliteConnection.Open();

    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseSqlite(sqliteConnection)
      .EnableDetailedErrors()
      .EnableSensitiveDataLogging()
      .Options;

    return new ApplicationDbContext(options);
  }
}
