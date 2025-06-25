using Hutch.Relay.Data.Entities;
using Hutch.Relay.Models;

namespace Hutch.Relay.Services.Contracts;

public interface ISubNodeService
{
  /// <summary>
  /// Check if there are any SubNodes configured
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown when there are no subnodes configured.</exception>
  public void EnsureSubNodes();

  /// <summary>
  /// Create a new SubNode associated with the provided user
  /// </summary>
  /// <param name="relayUser">The relay user registering the SubNode</param>
  /// <param name="specificId">An optional ID to use for the SubNode instead of generating one</param>
  /// <returns>The SubNode created.</returns>
  Task<SubNodeModel> Create(RelayUser relayUser, Guid? specificId = null);

  /// <summary>
  /// List all registered sub nodes
  /// </summary>
  /// <returns>A list of nodes</returns>
  Task<IEnumerable<SubNodeModel>> List();

  /// <summary>
  /// List all registered sub nodes for a given user
  /// </summary>
  /// <returns>A list of nodes for the requested user</returns>
  Task<IEnumerable<SubNodeModel>> List(string username);

  /// <summary>
  /// Delete a SubNode
  /// </summary>
  /// <param name="username">The Owner of the SubNode to be deleted</param>
  /// <param name="id">The ID of the SubNode to be deleted.</param>
  /// <returns></returns>
  Task Delete(string username, string id);
}
