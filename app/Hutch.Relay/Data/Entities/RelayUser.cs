using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Identity;

namespace Hutch.Relay.Data.Entities;

public class RelayUser : IdentityUser
{
  public ICollection<SubNode> SubNodes { get; set; } = [];

  /// <summary>
  /// Whether or not this User (and therefore their SubNodes) are declared in Relay configuration,
  /// or were added imperatively (e.g. via CLI)
  /// </summary>
  public bool IsDeclared { get; set; }
}
