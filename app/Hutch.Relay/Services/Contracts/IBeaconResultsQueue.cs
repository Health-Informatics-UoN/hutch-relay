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
}
