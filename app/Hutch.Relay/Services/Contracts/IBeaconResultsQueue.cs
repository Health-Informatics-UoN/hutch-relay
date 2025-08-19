namespace Hutch.Relay.Services.Contracts;

public interface IBeaconResultsQueue
{
  /// <summary>
  /// Publish the results of a given Beacon Individuals Availability Job to its queue
  /// </summary>
  /// <param name="jobId">The Job ID, expected in the form `{UniqueId}{BeaconSuffix}{QueueName}`</param>
  /// <param name="count">The count from the Availability Job final results</param>
  /// <returns></returns>
  Task Publish(string jobId, int count);

  /// <summary>
  /// Create a transient queue to be used for results for a single Beacon query.
  /// </summary>
  /// <returns>The name of the created queue</returns>
  Task<string> CreateResultsQueue();

  /// <summary>
  /// Connect to the specified queue and await results, returning them once received.
  /// </summary>
  /// <param name="queueName">The name of the queue to consume results from</param>
  /// <returns>The count from the results for the job this queue is for.</returns>
  Task<int> AwaitResults(string queueName);
}
