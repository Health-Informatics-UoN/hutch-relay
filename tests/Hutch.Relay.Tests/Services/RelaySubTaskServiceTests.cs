using Hutch.Relay.Constants;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hutch.Relay.Tests.Services;

public class RelaySubTaskServiceTests : IDisposable
{
  private readonly ApplicationDbContext _dbContext;

  public RelaySubTaskServiceTests()
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
  public async Task Create_ValidRelaySubTaskModel_ReturnsCreatedRelaySubTaskModel()
  {
    // Arrange
    var ownerId = Guid.NewGuid();
    var taskId = Guid.NewGuid().ToString();

    var subNode = new SubNode
    {
      Id = ownerId,
      RelayUsers = new List<RelayUser>
      {
        new() { Id = "test-user-id-1", UserName = "testuser1@example.com" }
      }
    };
    _dbContext.SubNodes.Add(subNode);

    var relayTask = new RelayTask
    {
      Id = taskId,
      Type = TaskTypes.TaskApi_Availability,
      Collection = "test-collection"
    };

    _dbContext.RelayTasks.Add(relayTask);

    await _dbContext.SaveChangesAsync();

    var service = new RelayTaskService(_dbContext);

    // Act
    var result = await service.CreateSubTask(taskId, ownerId);

    // Assert
    Assert.NotNull(result);

    var entityInDb = await _dbContext.RelaySubTasks.FindAsync(result.Id);
    Assert.NotNull(entityInDb);
  }

  [Fact]
  public async Task SetResult_ValidId_UpdatesResultAndReturnsRelaySubTaskModel()
  {
    // Arrange
    var subtaskId = Guid.NewGuid();
    var relaySubTask = new RelaySubTask
    {
      Id = subtaskId,
      RelayTask = new() { Id = "test-task-id-1", Type = TaskTypes.TaskApi_DemographicsDistribution, Collection = "" },
      Owner = new()
      {
        Id = Guid.NewGuid(),
        RelayUsers = new List<RelayUser>
        {
          new() { Id = "test-user-id-1", UserName = "testuser1@example.com" }
        }
      },
      Result = null
    };

    _dbContext.RelaySubTasks.Add(relaySubTask);
    await _dbContext.SaveChangesAsync();

    var service = new RelayTaskService(_dbContext);

    // Act
    const string updatedResult = "Test Result";
    var result = await service.SetSubTaskResult(subtaskId, updatedResult);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(subtaskId, result.Id);
    Assert.Equal(updatedResult, result.Result);

    var updatedSubTask = await _dbContext.RelaySubTasks
      .Include(st => st.Owner)
      .FirstOrDefaultAsync(st => st.Id == relaySubTask.Id);

    Assert.NotNull(updatedSubTask);
    Assert.Equal(updatedResult, updatedSubTask.Result);
  }
}
