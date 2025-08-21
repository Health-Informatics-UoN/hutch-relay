using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Hutch.Relay.Tests.Auth;

public class UserCollectionsFixture
{
  public readonly ApplicationDbContext Database;

  public static Guid SubNode1 = Guid.NewGuid();
  public static Guid SubNode2 = Guid.NewGuid();

  public static (string username, string password) User1 = ("user1", "password1");
  public static (string username, string password) User2 = ("user2", "password2");

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

  public UserCollectionsFixture()
  {
    // Set up a DB Context
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseInMemoryDatabase(databaseName: "UserCollectionsTestDatabase")
      .Options;

    Database = new(options);

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

    // Database.RelayUsers.AddRange(users);

    Database.SubNodes.AddRange(
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

    Database.SaveChanges();
  }
}
