namespace Hutch.Relay.Models;

public class RelayTaskModel
{
  public required string Id { get; set; }
  
  /// <summary>
  /// The type of the task; should be a value from <see cref="Constants.TaskTypes"/>
  /// </summary>
  public required string Type { get; set; }
  
  public required string Collection { get; set; }
  public DateTimeOffset CreatedAt { get; set; }
  public DateTimeOffset? CompletedAt { get; set; }
}
