namespace Hutch.Relay.Models;

public class RelayTaskModel
{
  public string Id { get; set; } = string.Empty;
  
  /// <summary>
  /// The type of the task; should be a value from <see cref="Constants.TaskTypes"/>
  /// </summary>
  public string Type { get; set; } = string.Empty;
  
  public string Collection { get; set; } = string.Empty;
  public DateTimeOffset CreatedAt { get; set; }
  public DateTimeOffset? CompletedAt { get; set; }
}
