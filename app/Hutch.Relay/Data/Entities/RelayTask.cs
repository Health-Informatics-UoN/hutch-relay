using System.ComponentModel.DataAnnotations;

namespace Hutch.Relay.Data.Entities;

public class RelayTask
{
  /// <summary>
  /// Manually provided as it keeps the upstream job id
  /// </summary>
  [MaxLength(255)] // Should be GUIDs (reasonably max 70)
  public string Id { get; set; } = string.Empty;

  [MaxLength(512)] // Type identifiers longer than this should be unreasonable :/
  public string Type { get; set; } = string.Empty;
  
  [MaxLength(255)] // Collection IDs should be GUIDs (reasonably max 70) or RQ IDs (reasonably max 50)
  public string Collection { get; set; } = string.Empty;
  
  public DateTimeOffset CreatedAt { get; set; }
  public DateTimeOffset? CompletedAt { get; set; }
  public List<RelaySubTask> SubTasks { get; set; } = [];
}
