namespace Hutch.Relay.Data.Entities;

public class RelayTask
{
  /// <summary>
  /// Manually provided as it keeps the upstream job id
  /// </summary>
  public string Id { get; set; } = string.Empty;
  public string Collection { get; set; } = string.Empty;
  public DateTimeOffset CreatedAt { get; set; }
  public DateTimeOffset? CompletedAt { get; set; }
  public List<RelaySubTask> SubTasks { get; set; } = [];
}
