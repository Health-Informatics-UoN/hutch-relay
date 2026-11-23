using Hutch.Relay.Config.Helpers;

namespace Hutch.Relay.Config;

[ConfigSection(Features.TypedQueues)]
public class TypedQueuesOptions : IFeatureOptionsModel
{
  /// <summary>
  /// If Enabled downstream jobs are published to separate queues for each task type.
  /// When disabled,downstream jobs are published to a single queue for each collection.
  /// </summary>
  public bool Enable { get; set; } = false;
}
