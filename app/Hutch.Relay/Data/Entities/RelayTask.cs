using System.ComponentModel.DataAnnotations;

namespace Hutch.Relay.Data.Entities;

public class RelayTask
{
  /// <summary>
  /// Manually provided as it keeps the upstream job id
  /// </summary>
  [MaxLength(255)] // Should be GUIDs (reasonably max 70)
  public required string Id { get; set; }

  [MaxLength(512)] // Type identifiers longer than this should be unreasonable :/
  public required string Type { get; set; }
  
  [MaxLength(255)] // Collection IDs should be GUIDs (reasonably max 70) or RQ IDs (reasonably max 50)
  public required string Collection { get; set; }
  
  public DateTimeOffset CreatedAt { get; set; }
  public DateTimeOffset? CompletedAt { get; set; }
  public List<RelaySubTask> SubTasks { get; set; } = [];
}
