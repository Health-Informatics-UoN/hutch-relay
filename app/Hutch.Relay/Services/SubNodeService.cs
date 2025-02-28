using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Models;
using Hutch.Relay.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hutch.Relay.Services;

public class SubNodeService(ApplicationDbContext db) : ISubNodeService
{
  /// <summary>
  /// Create a new SubNode associated with the provided user
  /// </summary>
  /// <param name="relayUser">The relay user registering the SubNode</param>
  /// <returns>The SubNode created.</returns>
  public async Task<SubNodeModel> Create(RelayUser relayUser)
  {
    var entity = new SubNode();
    entity.RelayUsers.Add(relayUser);
    db.SubNodes.Add(entity);
    await db.SaveChangesAsync();

    var model = new SubNodeModel
    {
      Id = entity.Id,
      Owner = entity.RelayUsers.First().UserName ?? string.Empty
    };
    return model;
  }

  /// <summary>
  /// List all registered sub nodes
  /// </summary>
  /// <returns>A list of nodes</returns>
  public async Task<IEnumerable<SubNodeModel>> List()
  {
    var entities = await db.SubNodes.AsNoTracking()
      .Include(x => x.RelayUsers)
      .ToListAsync();

    return entities.Select(x => new SubNodeModel
    {
      Id = x.Id,
      Owner = x.RelayUsers.First().UserName ?? string.Empty
    });
  }

  /// <summary>
  /// List all registered sub nodes for a given user
  /// </summary>
  /// <returns>A list of nodes for the requested user</returns>
  public async Task<IEnumerable<SubNodeModel>> List(string username)
  {
    var user = await db.RelayUsers.AsNoTracking().FirstOrDefaultAsync(x => x.UserName == username);
    if (user is null) throw new KeyNotFoundException($"Could not find user with the given username: {username}");

    var entities = await db.SubNodes.AsNoTracking()
      .Include(x => x.RelayUsers)
      .Where(x => x.RelayUsers.Contains(user))
      .ToListAsync();

    return entities.Select(x => new SubNodeModel
    {
      Id = x.Id,
      Owner = x.RelayUsers.First().UserName ?? string.Empty
    });
  }
}
