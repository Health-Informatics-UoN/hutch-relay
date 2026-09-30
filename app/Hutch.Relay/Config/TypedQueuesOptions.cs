using Hutch.Relay.Config.Helpers;

namespace Hutch.Relay.Config;

[ConfigSection(Features.TypedQueues)]
public class TypedQueuesOptions : IFeatureOptionsModel
{
  /// <summary>
  /// Whether downstream jobs are published to separate queues for each task type.
  /// </summary>
  /// <remarks>
  /// When disabled, Relay uses the original single queue for each collection.
  /// </remarks>
  public bool Enable { get; set; } = false;
}
