using System.Data.Common;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Hutch.Relay.Tests.Auth;

public class UserCollectionsFixture : IDisposable
{
  private readonly DbConnection? _connection = null;
  public readonly ApplicationDbContext DbContext;

  public static Guid SubNode1 = Guid.NewGuid();
  public static Guid SubNode2 = Guid.NewGuid();

  public static (string username, string password) User1 = ("user1", "password1");
  public static (string username, string password) User2 = ("user2", "password2");

  public UserCollectionsFixture()
  {
    // Ensure a unique DB per Test (does it, in a fixture?)
    DbContext = FixtureHelpers.NewDbContext(ref _connection);
    DbContext.Database.EnsureCreated();

    // Populate with some test data we can use for authorisation testing
    var hasher = new PasswordHasher<RelayUser>();
    List<RelayUser> users =
    [
      new()
      {
        Id = User1.username,
        UserName = User1.username, NormalizedUserName = User1.username.ToUpper(),
        SecurityStamp = "TEST_INIT_VALUE"
      },
      new()
      {
        Id = User2.username,
        UserName = User2.username, NormalizedUserName = User2.username.ToUpper(),
        SecurityStamp = "TEST_INIT_VALUE"
      }
    ];

    users[0].PasswordHash = hasher.HashPassword(users[0], User1.password);
    users[1].PasswordHash = hasher.HashPassword(users[1], User2.password);

    DbContext.SubNodes.AddRange(
      new()
      {
        Id = SubNode1,
        RelayUsers = [users[0]]
      },
      new()
      {
        Id = SubNode2,
        RelayUsers = [users[1]]
      });

    DbContext.SaveChanges();
  }

  public void Dispose()
  {
    DbContext.Database.EnsureDeleted();
    _connection?.Dispose();
  }

  public static IEnumerable<object[]> GetUserCollections()
  {
    yield return
    [
      User1.username, User1.password,
      SubNode1
    ];
    yield return
    [
      User2.username, User2.password,
      SubNode2
    ];
  }
}
