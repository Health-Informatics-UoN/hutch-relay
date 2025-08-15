namespace Hutch.Relay.Services.RabbitQueues;

public interface IBeaconResultsQueue
{
  /// <summary>
  /// Publish the results of a given Beacon Individuals Availability Job to its queue
  /// </summary>
  /// <param name="jobId">The Job ID, in the form `{BeaconPrefix}{QueueName}`</param>
  /// <param name="count">The count from the Availability Job final results</param>
  /// <returns></returns>
  Task Publish(string jobId, int count);
}
