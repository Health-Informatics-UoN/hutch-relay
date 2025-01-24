using Hutch.Relay.Models;

namespace Hutch.Relay.Services.Contracts;

public interface IRelayTaskService
{
  /// <summary>
  /// Get a RelayTask by id
  /// </summary>
  /// <param name="id">id of the RelayTask to get</param>
  /// <returns>The RelayTaskModel of the id</returns>
  /// <exception cref="KeyNotFoundException">The RelayTask does not exist.</exception>
  Task<RelayTaskModel> Get(string id);

  /// <summary>
  /// Create a new RelayTask
  /// </summary>
  /// <param name="model">Model to Create.</param>
  /// <returns>The newly created RelayTask.</returns>
  Task<RelayTaskModel> Create(RelayTaskModel model);

  /// <summary>
  /// Set a RelayTask as complete.
  /// </summary>
  /// <param name="id">The id of the task to complete.</param>
  /// <returns></returns>
  /// <exception cref="KeyNotFoundException">The RelayTask does not exist.</exception>
  Task<RelayTaskModel> SetComplete(string id);

  /// <summary>
  /// List RelayTasks that have not been completed.
  /// </summary>
  /// <returns>The list of incomplete RelayTasks</returns>
  Task<IEnumerable<RelayTaskModel>> ListIncomplete();

  /// <summary>
  /// Create a new RelaySubTask
  /// </summary>
  /// <returns>The newly created RelaySubTask</returns>
  Task<RelaySubTaskModel> CreateSubTask(string relayTaskId, Guid ownerId);

  /// <summary>
  /// Set the Result of a RelaySubTask
  /// </summary>
  /// <param name="id">id of the RelaySubTask</param>
  /// <param name="result">Result value to set</param>
  /// <returns>The updated RelaySubTask</returns>
  /// <exception cref="KeyNotFoundException"></exception>
  Task<RelaySubTaskModel> SetSubTaskResult(Guid id, string result);


  /// <summary>
  /// Get a SubTask by ID
  /// </summary>
  /// <param name="id">Subtask Id</param>
  /// <returns>RelaySubTaskModel from the ID</returns>
  /// <exception cref="KeyNotFoundException">The RelaySubTask does not exist.</exception>
  Task<RelaySubTaskModel> GetSubTask(Guid id);

  /// <summary>
  /// List RelaySubTasks that have not been completed for a given RelayTask.
  /// </summary>
  /// <returns>The list of incomplete RelaySubTasks</returns>
  Task<IEnumerable<RelaySubTaskModel>> ListSubTasks(string relayTaskId, bool incompleteOnly);
}
