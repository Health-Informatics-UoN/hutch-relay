namespace Hutch.Relay.Models;

public class RelaySubTaskModel
{
  public required Guid Id { get; set; }
  public required SubNodeModel Owner { get; set; }
  public required RelayTaskModel RelayTask { get; set; }
  public string? Result { get; set; }
}
