using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Identity;

namespace Hutch.Relay.Models;

public class RelayUserModel : IdentityUser
{
  public ICollection<SubNodeModel> SubNodes { get; set; } = new Collection<SubNodeModel>();
}
