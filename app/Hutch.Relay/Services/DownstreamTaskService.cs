using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Models;
using Hutch.Relay.Services.Contracts;

namespace Hutch.Relay.Services;

/// <summary>
/// A higher-level domain service for sending tasks downstream,
/// leveraging lower level services for datastore and queue interactions
/// </summary>
public class DownstreamTaskService(
  ILogger<DownstreamTaskService> logger,
  IDownstreamTaskQueue downstreamTaskQueue,
  IRelayTaskService relayTasks) : IDownstreamTaskService
{
  /// <summary>
  /// <para>Enqueue a task for a number of subnodes.</para>
  /// 
  /// <para>This method takes a Task API response model and creates a parent RelayTask in the datastore.</para>
  /// <para>Then for the specified SubNodes, created SubTasks in the datastore, and adds the downstream Tasks to the relevant queues.</para>
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="task"></param>
  /// <param name="targets"></param>
  /// <returns></returns>
  public async Task Enqueue<T>(T task, List<SubNodeModel> targets)
    where T : TaskApiBaseResponse
  {
    // Make sure there are some targets; leave if not
    if (targets.Count == 0)
    {
      logger.LogWarning("No Subnodes are configured; not enqueueing Task.");
      return;
    }

    var relayTask = await relayTasks.Create(new()
    {
      Id = task.Uuid,
      Type = IRelayTaskService.GetTaskApiType(task),
      Collection = task.Collection
    });

    // Fan out to subtasks
    foreach (var subnode in targets)
    {
      var subTask = await relayTasks.CreateSubTask(relayTask.Id, subnode.Id);

      // Update the job for the target subnode
      task.Uuid = subTask.Id.ToString();
      task.Collection = subnode.Id.ToString();
      task.Owner = subnode.Owner;

      // Queue the task for the subnode
      await downstreamTaskQueue.Publish(subnode.Id.ToString(), task);
    }
  }
}
