using Hutch.Relay.Config;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services.DeclarativeConfigServiceTests;

public class ReconcileDownstreamUsersTests : IDisposable
{
  private readonly ApplicationDbContext _dbContext;
  private readonly UpperInvariantLookupNormalizer _normalizer = new();

  private readonly string _imperativeClashUsername = "imperative-user1";
  private readonly Guid _imperativeClashGuid = Guid.NewGuid();

  public ReconcileDownstreamUsersTests()
  {
    // Ensure a unique DB per Test
    _dbContext = FixtureHelpers.NewDbContext($"Test_{Guid.NewGuid()}");
    _dbContext.Database.EnsureCreated();

    // Always add some existing imperative config for these tests
    _dbContext.RelayUsers.AddRange([
      new RelayUser {
         UserName = _imperativeClashUsername,
         NormalizedUserName = _normalizer.NormalizeName(_imperativeClashUsername),
         SubNodes = [new() { Id = Guid.NewGuid() }]
      },
      new RelayUser {
         UserName = "imperative-user2",
         NormalizedUserName = _normalizer.NormalizeName("imperative-user2"),
         SubNodes = [new() { Id = _imperativeClashGuid }]
      },
    ]);

    _dbContext.SaveChanges();
    _dbContext.ChangeTracker.Clear(); // stop tracking changes from arranging test data
  }

  // https://stackoverflow.com/a/52562694
  public Mock<UserManager<RelayUser>> MockUserManager()
  {
    var mgr = new Mock<UserManager<RelayUser>>(
      new UserStore<RelayUser>(_dbContext), // Backed by our test db context
      null!, null!, null!, null!, null!, null!, null!, null!);

    mgr.Object.UserValidators.Add(new UserValidator<RelayUser>());
    mgr.Object.PasswordValidators.Add(new PasswordValidator<RelayUser>());

    mgr.Setup(x => x.DeleteAsync(It.IsAny<RelayUser>()))
    .ReturnsAsync(IdentityResult.Success)
    .Callback<RelayUser>(user =>
    {
      _dbContext.RelayUsers.Remove(user);
      _dbContext.SaveChanges();
    });

    mgr.Setup(x => x.CreateAsync(It.IsAny<RelayUser>(), It.IsAny<string>()))
      .ReturnsAsync(IdentityResult.Success)
      .Callback<RelayUser, string>((user, pass) =>
      {
        user.PasswordHash = pass;
        user.NormalizedUserName = _normalizer.NormalizeName(user.UserName);
        _dbContext.RelayUsers.Add(user);
        _dbContext.SaveChanges();
      });

    mgr.Setup(x => x.UpdateAsync(It.IsAny<RelayUser>()))
      .ReturnsAsync(IdentityResult.Success)
      .Callback<RelayUser>(user =>
      {
        _dbContext.RelayUsers.Update(user);
        _dbContext.SaveChanges();
      });

    mgr.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
      .ReturnsAsync((string username) => _dbContext.RelayUsers
        .FirstOrDefault(x => x.NormalizedUserName == _normalizer.NormalizeName(username)));

    mgr.Setup(x => x.CheckPasswordAsync(It.IsAny<RelayUser>(), It.IsAny<string>()))
      .ReturnsAsync((RelayUser user, string password) => user.PasswordHash == password);

    mgr.Setup(x => x.GeneratePasswordResetTokenAsync(It.IsAny<RelayUser>()))
      .ReturnsAsync("token");

    mgr.Setup(x => x.ResetPasswordAsync(It.IsAny<RelayUser>(), It.IsAny<string>(), It.IsAny<string>()))
      .ReturnsAsync(IdentityResult.Success)
      .Callback((RelayUser user, string token, string password) =>
      {
        user.PasswordHash = password;
        _dbContext.RelayUsers.Update(user);
        _dbContext.SaveChanges();
      });

    return mgr;
  }

  public Mock<ISubNodeService> MockSubNodeService()
  {
    var subnodesMock = new Mock<ISubNodeService>();
    subnodesMock.Setup(x => x.Create(It.IsAny<RelayUser>(), It.IsAny<Guid?>()))
      .ReturnsAsync((RelayUser user, Guid? id) =>
      {
        var subnode = new SubNode() { RelayUsers = [user] };
        if (id is not null) subnode.Id = id.Value;

        _dbContext.SubNodes.Add(subnode);
        _dbContext.SaveChanges();
        return new()
        {
          Id = subnode.Id,
          Owner = user.UserName!
        };
      });

    subnodesMock.Setup(x => x.Delete(It.IsAny<string>(), It.IsAny<string>()))
      .Callback((string username, string id) =>
      {
        var subnode = _dbContext.SubNodes.SingleOrDefault(x => x.Id == Guid.Parse(id));
        if (subnode is null) return;

        _dbContext.SubNodes.Remove(subnode);
        _dbContext.SaveChanges();
      });

    return subnodesMock;
  }

  public void Dispose()
  {
    _dbContext.Database.EnsureDeleted();
  }

  [Fact] // Empty declarative config preserves imperative config
  public async Task ReconcileDownstreamUsers_EmptyConfig_PreservesExistingImperativeConfig()
  {
    // Arrange
    var userManager = MockUserManager().Object;
    var subnodes = MockSubNodeService().Object;

    var service = new DeclarativeConfigService(
      Options.Create<DownstreamUsersOptions>(new()),
      userManager, _dbContext, _normalizer, subnodes);


    // Act
    await service.ReconcileDownstreamUsers(); // This should essentially do nothing for this test


    // Assert
    Assert.Equal(2, _dbContext.RelayUsers.Count()); // Expect the seeded imperative users to be there only
    Assert.Equal(2, _dbContext.SubNodes.Count()); // And their subnodes
  }

  [Fact] // Empty declarative config clears previous declarative config
  public async Task ReconcileDownstreamUsers_EmptyConfig_ClearsPreviousDeclarativeConfig()
  {
    // Arrange
    var userManager = MockUserManager().Object;
    var subnodes = MockSubNodeService().Object;

    var service = new DeclarativeConfigService(
      Options.Create<DownstreamUsersOptions>(new()),
      userManager, _dbContext, _normalizer, subnodes);

    // Add some extra Declaratively added users and nodes
    _dbContext.RelayUsers.Add(new()
    {
      IsDeclared = true,
      UserName = "declarative-user",
      NormalizedUserName = _normalizer.NormalizeName("declarative-user"),
      SubNodes = [new() { Id = Guid.NewGuid() }]
    });
    _dbContext.SaveChanges();
    _dbContext.ChangeTracker.Clear(); // stop tracking changes from arranging test data <3


    // Act
    await service.ReconcileDownstreamUsers(); // should remove the declared data only


    // Assert
    Assert.Equal(2, _dbContext.RelayUsers.Count()); // Expect the seeded imperative users to be there only
    Assert.Equal(2, _dbContext.SubNodes.Count()); // And their subnodes
  }

  [Fact] // New declarative user adds declarative user
  public async Task ReconcileDownstreamUsers_NewUser_AddsNewUser()
  {
    // Arrange
    var inputUsername = "declarative-user";
    var inputPassword = "abc123";

    var userManager = MockUserManager().Object;
    var subnodes = MockSubNodeService().Object;

    var downstreamUsersConfig = new DownstreamUsersOptions()
    {
      [inputUsername] = new() { Password = inputPassword }
    };

    var service = new DeclarativeConfigService(
      Options.Create(downstreamUsersConfig),
      userManager, _dbContext, _normalizer, subnodes);

    // Act
    await service.ReconcileDownstreamUsers();

    // Assert
    var createdUser = _dbContext.RelayUsers.AsNoTracking()
      .Single(x => x.UserName == inputUsername);

    Assert.Equal(inputUsername, createdUser.UserName);
    Assert.Equal(_normalizer.NormalizeName(inputUsername), createdUser.NormalizedUserName);
    Assert.Equal(inputPassword, createdUser.PasswordHash);
    Assert.True(createdUser.IsDeclared);
  }

  [Fact] // New user and subnode adds subnode
  public async Task ReconcileDownstreamUsers_NewUserWithSubNode_AddsNewSubNode()
  {
    // Arrange
    var inputUsername = "declarative-user";
    var inputPassword = "abc123";
    var inputSubnodeId = Guid.NewGuid();

    var userManager = MockUserManager().Object;
    var subnodes = MockSubNodeService().Object;

    var downstreamUsersConfig = new DownstreamUsersOptions()
    {
      [inputUsername] = new()
      {
        Password = inputPassword,
        SubNode = inputSubnodeId
      }
    };

    var service = new DeclarativeConfigService(
      Options.Create(downstreamUsersConfig),
      userManager, _dbContext, _normalizer, subnodes);

    // Act
    await service.ReconcileDownstreamUsers();

    // Assert
    var createdUser = _dbContext.RelayUsers.AsNoTracking()
      .Include(x => x.SubNodes)
      .Single(x => x.UserName == inputUsername);

    Assert.Contains(createdUser.SubNodes, x => x.Id == inputSubnodeId);
  }

  [Fact] // New subnode for existing user adds subnode
  public async Task ReconcileDownstreamUsers_ExistingUser_AddsNewSubNode()
  {
    // Arrange
    var inputUsername = "declarative-user";
    var inputPassword = "abc123";
    var inputSubnodeId = Guid.NewGuid();

    var userManager = MockUserManager().Object;
    var subnodes = MockSubNodeService().Object;

    var downstreamUsersConfig = new DownstreamUsersOptions()
    {
      [inputUsername] = new()
      {
        Password = inputPassword,
        SubNodes = [inputSubnodeId]
      }
    };

    // Pre-add our declarative user
    _dbContext.RelayUsers.Add(new()
    {
      IsDeclared = true,
      UserName = inputUsername,
      NormalizedUserName = _normalizer.NormalizeName(inputUsername)
    });
    _dbContext.SaveChanges();
    _dbContext.ChangeTracker.Clear(); // stop tracking changes from arranging test data <3

    var service = new DeclarativeConfigService(
      Options.Create(downstreamUsersConfig),
      userManager, _dbContext, _normalizer, subnodes);

    // Act
    await service.ReconcileDownstreamUsers();

    // Assert
    var createdUser = _dbContext.RelayUsers.AsNoTracking()
      .Include(x => x.SubNodes)
      .Single(x => x.UserName == inputUsername);

    Assert.Contains(createdUser.SubNodes, x => x.Id == inputSubnodeId);
  }

  [Fact] // Change user password works
  public async Task ReconcileDownstreamUsers_ExistingUserNewPassword_ChangesPassword()
  {
    // Arrange
    var existingUsername = "declarative-user";
    var newPassword = "def456";

    var userManager = MockUserManager().Object;
    var subnodes = MockSubNodeService().Object;

    var downstreamUsersConfig = new DownstreamUsersOptions()
    {
      [existingUsername] = new()
      {
        Password = newPassword
      }
    };

    // Pre-add our declarative user
    _dbContext.RelayUsers.Add(new()
    {
      IsDeclared = true,
      UserName = existingUsername,
      NormalizedUserName = _normalizer.NormalizeName(existingUsername)
    });
    _dbContext.SaveChanges();
    _dbContext.ChangeTracker.Clear(); // stop tracking changes from arranging test data <3

    var service = new DeclarativeConfigService(
      Options.Create(downstreamUsersConfig),
      userManager, _dbContext, _normalizer, subnodes);

    // Act
    await service.ReconcileDownstreamUsers();

    // Assert
    var createdUser = _dbContext.RelayUsers.AsNoTracking()
      .Single(x => x.UserName == existingUsername);

    Assert.Equal(newPassword, createdUser.PasswordHash);
  }

  [Fact] // remove existing subnodes from existing user
  public async Task ReconcileDownstreamUsers_ExistingUserWithSubNodes_RemovesMissingSubNode()
  {
    // Arrange
    var existingUsername = "declarative-user";
    var existingPassword = "abc123";
    var existingSubnodeId = Guid.NewGuid();
    var removedSubnodeId = Guid.NewGuid();

    var userManager = MockUserManager().Object;
    var subnodes = MockSubNodeService().Object;

    var downstreamUsersConfig = new DownstreamUsersOptions()
    {
      [existingUsername] = new()
      {
        Password = existingPassword,
        SubNodes = [existingSubnodeId]
      }
    };

    // Pre-add our declarative user
    _dbContext.RelayUsers.Add(new()
    {
      IsDeclared = true,
      UserName = existingUsername,
      NormalizedUserName = _normalizer.NormalizeName(existingUsername),
      SubNodes = [
        new() { Id = existingSubnodeId },
        new() { Id = removedSubnodeId }
      ]
    });
    _dbContext.SaveChanges();
    _dbContext.ChangeTracker.Clear(); // stop tracking changes from arranging test data <3

    var service = new DeclarativeConfigService(
      Options.Create(downstreamUsersConfig),
      userManager, _dbContext, _normalizer, subnodes);

    // Act
    await service.ReconcileDownstreamUsers();

    // Assert
    var createdUser = _dbContext.RelayUsers.AsNoTracking()
      .Include(x => x.SubNodes)
      .Single(x => x.UserName == existingUsername);

    var removedSubNode = _dbContext.SubNodes.AsNoTracking().SingleOrDefault(x => x.Id == removedSubnodeId);

    Assert.Contains(createdUser.SubNodes, x => x.Id == existingSubnodeId);
    Assert.DoesNotContain(createdUser.SubNodes, x => x.Id == removedSubnodeId);
    Assert.Null(removedSubNode);
  }

  [Fact] // Subnode id conflict within declarative config
  public async Task ReconcileDownstreamUsers_SubNodeIdClashInConfig_Throws()
  {
    // Arrange
    var clashingSubnodeId = Guid.NewGuid();

    var userManager = MockUserManager().Object;
    var subnodes = MockSubNodeService().Object;

    var downstreamUsersConfig = new DownstreamUsersOptions()
    {
      ["declarative-user1"] = new()
      {
        Password = "",
        SubNodes = [clashingSubnodeId]
      },
      ["declarative-user2"] = new()
      {
        Password = "",
        SubNodes = [clashingSubnodeId]
      }
    };

    var service = new DeclarativeConfigService(
      Options.Create(downstreamUsersConfig),
      userManager, _dbContext, _normalizer, subnodes);

    // Act / Assert
    await Assert.ThrowsAsync<InvalidOperationException>(service.ReconcileDownstreamUsers);
  }

  [Fact] // Subnode id conflict with existing imperative config
  public async Task ReconcileDownstreamUsers_ExistingSubNodeIdClash_Throws()
  {
    // Arrange
    var userManager = MockUserManager().Object;
    var subnodes = MockSubNodeService().Object;

    var downstreamUsersConfig = new DownstreamUsersOptions()
    {
      ["declarative-user1"] = new()
      {
        Password = "",
        SubNodes = [_imperativeClashGuid]
      },
    };

    var service = new DeclarativeConfigService(
      Options.Create(downstreamUsersConfig),
      userManager, _dbContext, _normalizer, subnodes);

    // Act / Assert
    await Assert.ThrowsAsync<InvalidOperationException>(service.ReconcileDownstreamUsers);
  }

  [Fact] // Declarative username conflict with existing imperative config
  public async Task ReconcileDownstreamUsers_ExistingImperativeUsernameClash_Throws()
  {
    // Arrange
    var userManager = MockUserManager().Object;
    var subnodes = MockSubNodeService().Object;

    var downstreamUsersConfig = new DownstreamUsersOptions()
    {
      [_imperativeClashUsername] = new()
      {
        Password = "",
      },
    };

    var service = new DeclarativeConfigService(
      Options.Create(downstreamUsersConfig),
      userManager, _dbContext, _normalizer, subnodes);

    // Act / Assert
    await Assert.ThrowsAsync<InvalidOperationException>(service.ReconcileDownstreamUsers);
  }
}
