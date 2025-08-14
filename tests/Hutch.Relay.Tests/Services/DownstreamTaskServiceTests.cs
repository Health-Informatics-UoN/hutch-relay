using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Constants;
using Hutch.Relay.Models;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services;

public class DownstreamTaskServiceTests
{
  [Fact]
  public async Task Enqueue_WithAvailabilityTask_CreatesStateAndQueues()
  {
    // Arrange
    var availabilityTask = new AvailabilityJob();
    var relayTask = new RelayTaskModel()
    {
      Id = Guid.NewGuid().ToString(),
      Type = TaskTypes.TaskApi_Availability,
      Collection = "test",
    };
    var relaySubTask = new RelaySubTaskModel()
    {
      Id = Guid.NewGuid(),
      Owner = new()
      {
        Id = Guid.NewGuid(),
        Owner = "user"
      },
      RelayTask = relayTask
    };

    var subnodes = new List<SubNodeModel>([relaySubTask.Owner]);

    var tasks = new Mock<IRelayTaskService>();
    var taskDb = new List<RelayTaskModel>();
    tasks.Setup(x =>
        x.Create(It.IsAny<RelayTaskModel>()))
      .Returns(() =>
      {
        taskDb.Add(relayTask);
        return Task.FromResult(relayTask);
      });

    var subtaskDb = new List<RelaySubTaskModel>();
    tasks.Setup(x =>
        x.CreateSubTask(relayTask.Id, relaySubTask.Owner.Id))
      .Returns(() =>
      {
        subtaskDb.Add(relaySubTask);
        return Task.FromResult(relaySubTask);
      });

    var logger = Mock.Of<ILogger<DownstreamTaskService>>();

    var queues = new Mock<IDownstreamTaskQueue>();
    var queue = new List<AvailabilityJob>();
    queues.Setup(x =>
      x.IsReady(It.IsAny<string>())).Returns(Task.FromResult(true));
    queues.Setup(x => x.Send(relaySubTask.Owner.Id.ToString(), availabilityTask)).Returns(() =>
    {
      queue.Add(availabilityTask);
      return Task.CompletedTask;
    });

    var service = new DownstreamTaskService(logger, queues.Object, tasks.Object);

    // Act
    await service.Enqueue(availabilityTask, subnodes);

    // Assert
    Assert.Multiple(() =>
    {
      Assert.Single(taskDb);
      Assert.Single(subtaskDb);
      Assert.Single(queue);

      Assert.Contains(relayTask, taskDb);
      Assert.Contains(relaySubTask, subtaskDb);
      Assert.Contains(availabilityTask, queue);
    });
  }

  // TODO: Test for no subnodes early return
  [Fact]
  public async Task Enqueue_WhenNoSubnodes_DoesNotEnqueue()
  {
    List<SubNodeModel> subnodes = [];

    var logger = Mock.Of<ILogger<DownstreamTaskService>>();

    var tasks = new Mock<IRelayTaskService>();

    var queues = new Mock<IDownstreamTaskQueue>();

    var service = new DownstreamTaskService(logger, queues.Object, tasks.Object);

    // Act
    await service.Enqueue(new AvailabilityJob(), subnodes);

    // Assert
    tasks.Verify(x => x.Create(It.IsAny<RelayTaskModel>()), Times.Never);
    tasks.Verify(x => x.Create(It.IsAny<RelayTaskModel>()), Times.Never);
    queues.Verify(x => x.Send(It.IsAny<string>(), It.IsAny<AvailabilityJob>()), Times.Never);
  }
}
