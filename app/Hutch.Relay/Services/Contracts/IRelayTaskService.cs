using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Constants;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Models;

namespace Hutch.Relay.Services.Contracts;

public interface IRelayTaskService
{
  #region TaskType Helpers

  // In future we can have other helpers for other Task Type sources if necessary

  /// <summary>
  /// Get the RelayTask Type value from a Task API job
  /// </summary>
  /// <param name="task">The payload received from the Task API as a <see cref="TaskApiBaseResponse"/> derivative.</param>
  /// <typeparam name="T">The specific job type of the <see cref="TaskApiBaseResponse"/></typeparam>
  /// <returns>a standardised <see cref="TaskTypes"/> name for Relay to store on the <see cref="RelayTask"/> for later use.</returns>
  /// <exception cref="ArgumentOutOfRangeException"></exception>
  public static string GetTaskApiType<T>(T task) where T : TaskApiBaseResponse
  {
    return task switch
    {
      AvailabilityJob
        => TaskTypes.TaskApi_Availability,

      CollectionAnalysisJob { Analysis: AnalysisType.Distribution, Code: DistributionCode.Generic }
        => TaskTypes.TaskApi_CodeDistribution,

      CollectionAnalysisJob { Analysis: AnalysisType.Distribution, Code: DistributionCode.Demographics }
        => TaskTypes.TaskApi_DemographicsDistribution,

      CollectionAnalysisJob { Analysis: AnalysisType.Distribution, Code: DistributionCode.Icd }
        => throw new ArgumentOutOfRangeException(nameof(task), task, "Unsupported Task API Task Type: ICD-MAIN Distribution"),

      _ => throw new ArgumentOutOfRangeException(nameof(task), task, "Unsupported Task API Task Type")
    };
  }

  #endregion
  
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
