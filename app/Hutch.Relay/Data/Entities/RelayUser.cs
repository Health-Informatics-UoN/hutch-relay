using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Identity;

namespace Hutch.Relay.Data.Entities;

public class RelayUser: IdentityUser
{
  public ICollection<SubNode> SubNodes { get; set; } = new Collection<SubNode>();
}
