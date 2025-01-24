using System.Collections.ObjectModel;

namespace Hutch.Relay.Models;

public class SubNodeModel
{
  public required Guid Id { get; init; }

  public required string Owner { get; init; }
}
