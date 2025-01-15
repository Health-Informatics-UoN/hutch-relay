using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Models;
using Hutch.Relay.Services;
using Xunit;
using Assert = Xunit.Assert;

namespace Hutch.Relay.Tests.Services;

public class RelayTaskServiceTests(Fixtures fixtures) : IClassFixture<Fixtures>, IAsyncLifetime
{
  private readonly ApplicationDbContext _dbContext = fixtures.DbContext;

  public Task InitializeAsync()
  {
    return Task.CompletedTask;
  }
  public async Task DisposeAsync()
  {
    // Clean up the database after each test
    _dbContext.RelayTasks.RemoveRange(_dbContext.RelayTasks);
    await _dbContext.SaveChangesAsync();
  }
  
  [Fact]
  public async Task Get_WithValidId_ReturnsRelayTaskModel()
  {
    // Arrange
    var relayTask = new RelayTask
    {
      Id = "valid-id-1",
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
      Collection = "New Collection"
    };

    var service = new RelayTaskService(_dbContext);

    // Act
    var result = await service.Create(model);

    // Assert
    Assert.NotNull(result);
    Assert.NotNull(result.Id);
    Assert.Equal(model.Collection, result.Collection);
    Assert.Null(result.CompletedAt);

    var entityInDb = await _dbContext.RelayTasks.FindAsync(result.Id);
    Assert.NotNull(entityInDb);
    Assert.Equal(model.Collection, entityInDb.Collection);
  }
  
  [Fact]
  public async Task SetComplete_ValidId_UpdatesCompletedAtAndReturnsRelayTaskModel()
  {
    // Arrange
    var relayTask = new RelayTask
    {
      Id = "valid-id-2",
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
      Collection = "Collection 1"
    };
    var incompleteTask2 = new RelayTask
    {
      Id = "incomplete-id-2",
      Collection = "Collection 2"
    };
    var completedTask = new RelayTask
    {
      Id = "completed-id",
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
