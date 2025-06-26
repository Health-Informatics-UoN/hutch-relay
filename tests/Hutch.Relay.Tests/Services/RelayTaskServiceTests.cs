using Hutch.Relay.Constants;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Models;
using Hutch.Relay.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hutch.Relay.Tests.Services;

public class RelayTaskServiceTests : IDisposable
{
  private readonly ApplicationDbContext _dbContext;

  public RelayTaskServiceTests()
  {
    // Ensure a unique DB per Test
    _dbContext = FixtureHelpers.NewDbContext($"Test_{Guid.NewGuid()}");
    _dbContext.Database.EnsureCreated();
  }

  public void Dispose()
  {
    _dbContext.Database.EnsureDeleted();
  }

  [Fact]
  public async Task Delete_WithValidId_DeletesRelayTask()
  {
    // Arrange
    var relayTask = new RelayTask
    {
      Id = "valid-id-1",
      Type = TaskTypes.TaskApi_Availability,
      Collection = "Sample Collection"
    };

    _dbContext.RelayTasks.Add(relayTask);
    await _dbContext.SaveChangesAsync();

    var service = new RelayTaskService(_dbContext);

    // Act
    await service.Delete(relayTask.Id);

    // Assert
    var result = await _dbContext.RelayTasks.FindAsync(relayTask.Id);
    Assert.Null(result);
  }

  [Fact]
  public async Task Delete_WithValidId_DeletesRelaySubTasks()
  {
    // Arrange
    List<Guid> subtaskIds = [Guid.NewGuid(), Guid.NewGuid()];

    var relayTask = new RelayTask
    {
      Id = "test-task-id-1",
      Type = TaskTypes.TaskApi_DemographicsDistribution,
      Collection = ""
    };
    var owner = new SubNode() { Id = Guid.NewGuid(), RelayUsers = [new() { Id = "test-user-id-1", UserName = "testuser1@example.com" }] };

    foreach (var subtaskId in subtaskIds)
    {
      var relaySubTask = new RelaySubTask
      {
        Id = subtaskId,
        RelayTask = relayTask,
        Owner = owner,
        Result = null
      };
      _dbContext.RelaySubTasks.Add(relaySubTask);
    }
    _dbContext.RelayTasks.Add(relayTask);
    await _dbContext.SaveChangesAsync();

    var service = new RelayTaskService(_dbContext);

    // Act
    await service.Delete(relayTask.Id);

    // Assert
    foreach (var subtaskId in subtaskIds)
    {
      Assert.Null(await _dbContext.RelaySubTasks.FindAsync(subtaskId));
    }
  }

  [Fact]
  public async Task Delete_WithInvalidId_DoesNotThrow()
  {
    // Arrange
    var invalidId = "invalid-id";
    var service = new RelayTaskService(_dbContext);

    // Act / Assert
    await service.Delete(invalidId);

    // Assert
    var result = await _dbContext.RelayTasks.FindAsync(invalidId);
    Assert.Null(result);
  }

  [Fact]
  public async Task Get_WithValidId_ReturnsRelayTaskModel()
  {
    // Arrange
    var relayTask = new RelayTask
    {
      Id = "valid-id-1",
      Type = TaskTypes.TaskApi_Availability,
      Collection = "Sample Collection"
    };
    _dbContext.RelayTasks.Add(relayTask);
    await _dbContext.SaveChangesAsync();

    var service = new RelayTaskService(_dbContext);

    // Act
    var result = await service.Get("valid-id-1");

    // Assert
    Assert.NotNull(result);
    Assert.Equal(relayTask.Id, result.Id);
    Assert.Equal(relayTask.Type, result.Type);
    Assert.Equal(relayTask.Collection, result.Collection);
  }

  [Fact]
  public async Task Get_WithInvalidId_ThrowsKeyNotFoundException()
  {
    // Arrange
    var service = new RelayTaskService(_dbContext);

    // Act / Assert
    await Assert.ThrowsAsync<KeyNotFoundException>(() => service.Get("DoesNotExist"));
  }

  [Fact]
  public async Task Create_ValidRelayTaskModel_ReturnsCreatedRelayTaskModel()
  {
    // Arrange
    var model = new RelayTaskModel
    {
      Id = "valid-id-7",
      Type = TaskTypes.TaskApi_CodeDistribution,
      Collection = "New Collection"
    };

    var service = new RelayTaskService(_dbContext);

    // Act
    var result = await service.Create(model);

    // Assert
    Assert.NotNull(result);
    Assert.NotNull(result.Id);
    Assert.Equal(model.Collection, result.Collection);
    Assert.Equal(model.Type, result.Type);
    Assert.Null(result.CompletedAt);

    var entityInDb = await _dbContext.RelayTasks.FindAsync(result.Id);
    Assert.NotNull(entityInDb);
    Assert.Equal(model.Collection, entityInDb.Collection);
    Assert.Equal(model.Type, entityInDb.Type);
  }

  [Fact]
  public async Task SetComplete_ValidId_UpdatesCompletedAtAndReturnsRelayTaskModel()
  {
    // Arrange
    var relayTask = new RelayTask
    {
      Id = "valid-id-2",
      Type = TaskTypes.TaskApi_DemographicsDistribution,
      Collection = "Sample Collection",
    };
    _dbContext.RelayTasks.Add(relayTask);
    await _dbContext.SaveChangesAsync();

    var service = new RelayTaskService(_dbContext);

    // Act
    var result = await service.SetComplete("valid-id-2");

    // Assert
    Assert.NotNull(result.CompletedAt);

    var entityInDb = await _dbContext.RelayTasks.FindAsync("valid-id-2");
    Assert.NotNull(entityInDb);
    Assert.NotNull(entityInDb.CompletedAt);
  }

  [Fact]
  public async Task ListIncomplete_ReturnsOnlyIncompleteTasks()
  {
    // Arrange - (2 incomplete, 1 complete)
    var incompleteTask1 = new RelayTask
    {
      Id = "incomplete-id-1",
      Type = TaskTypes.TaskApi_Availability,
      Collection = "Collection 1"
    };
    var incompleteTask2 = new RelayTask
    {
      Id = "incomplete-id-2",
      Type = TaskTypes.TaskApi_DemographicsDistribution,
      Collection = "Collection 2"
    };
    var completedTask = new RelayTask
    {
      Id = "completed-id",
      Type = TaskTypes.TaskApi_CodeDistribution,
      Collection = "Collection 3",
      CompletedAt = DateTime.UtcNow.AddMinutes(3)
    };

    _dbContext.RelayTasks.AddRange(incompleteTask1, incompleteTask2, completedTask);
    await _dbContext.SaveChangesAsync();

    var service = new RelayTaskService(_dbContext);

    // Act
    var result = await service.ListIncomplete();

    // Assert
    Assert.NotNull(result);
    Assert.Equal(2, result.Count());

    var incompleteTasks = result.ToList();
    Assert.Contains(incompleteTasks, x => x.Id == incompleteTask1.Id);
    Assert.Contains(incompleteTasks, x => x.Id == incompleteTask2.Id);
    Assert.DoesNotContain(incompleteTasks, x => x.Id == completedTask.Id);
  }
}
