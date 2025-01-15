using System.Collections.ObjectModel;

namespace Hutch.Relay.Data.Entities;

public class SubNode
{
  public Guid Id { get; set; }

  public ICollection<RelayUser> RelayUsers { get; set; } = new Collection<RelayUser>();
}
