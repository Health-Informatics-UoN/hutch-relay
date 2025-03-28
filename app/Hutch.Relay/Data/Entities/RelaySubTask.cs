using System.ComponentModel.DataAnnotations;

namespace Hutch.Relay.Data.Entities;

public class RelaySubTask
{
  public Guid Id { get; set; }
  public required SubNode Owner { get; set; }
  public required RelayTask RelayTask { get; set; }
  public string? Result { get; set; }
}
