namespace Hutch.Relay.Data.Entities;

public class RelaySubTask
{
  public Guid Id { get; set; }
  public SubNode Owner { get; set; } = new();
  public RelayTask RelayTask { get; set; } = new();
  public string? Result { get; set; }
}
