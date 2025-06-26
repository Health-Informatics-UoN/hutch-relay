using Hutch.Relay.Config;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Services;

/// <summary>
/// Service for reconciling declaratively configured state with the current local datastore
/// </summary>
public class DeclarativeConfigService(
  IOptions<DownstreamUsersOptions> downstreamUsersOptions,
  UserManager<RelayUser> users,
  ApplicationDbContext db,
  ILookupNormalizer normalizer,
  ISubNodeService subnodes)
{
  private readonly DownstreamUsersOptions _downstreamUsers = downstreamUsersOptions.Value;

  public async Task ReconcileDownstreamUsers()
  {
    var existingUsers = await db.RelayUsers.AsNoTracking()
      .Include(x => x.SubNodes)
      .ToListAsync();

    // normalise our declared usernames the same way aspnet identity does
    var normalisedDeclaredUsernames = _downstreamUsers.DownstreamUsers.Keys
      .Select(x => normalizer.NormalizeName(x));

    // Start by deleting undeclared users, and their subnodes too
    foreach (var existingUser in existingUsers)
    {

      // not sure how this can practically happen,
      // but if they don't have a username we can't username match them anyway
      if (existingUser.NormalizedUserName is null) continue;

      if (!normalisedDeclaredUsernames.Contains(existingUser.NormalizedUserName))
      {
        if (existingUser.IsDeclared) // Only remove users previously declared; keep manually added ones
        {
          foreach (var subnode in existingUser.SubNodes)
          {
            // Only delete if we're the only user for the subnode
            if (subnode.RelayUsers.Count == 1)
              db.SubNodes.Remove(subnode);
          }
          await users.DeleteAsync(existingUser);
        }
      }
    }

    // Then foreach declared user, add them if they don't exist, or update them (including subnodes) if they do
    foreach (var (username, details) in _downstreamUsers.DownstreamUsers)
    {
      // Prepare the configured subnodes collection
      details.SubNodes ??= [];
      if (details.SubNode is not null)
        details.SubNodes.Add(details.SubNode.Value); // Merge single subnode if applicable

      // We don't use UserManager's FindByName to get the user
      // since we want to include SubNodes
      // We don't work from `existingUsers` since we a) want to make sure we're up to date and b) want to track changes now
      var user = await db.RelayUsers
        .Include(x => x.SubNodes)
        .SingleOrDefaultAsync(x => x.NormalizedUserName == normalizer.NormalizeName(username));

      var shouldCreate = user is null;

      if (!shouldCreate)
      {
        // Terminate if this user already exists imperatively!
        if (!user!.IsDeclared)
          throw new InvalidOperationException(
            $"A Downstream User declared in config already exists from being added manually. Please change the config or manually remove the user '{username}'");

        // Delete subnodes we don't want anymore
        List<Guid> subnodesToDelete = []; // So we don't delete while enumerating
        foreach (var subnode in user.SubNodes)
          if (!details.SubNodes!.Contains(subnode.Id))
            subnodesToDelete.Add(subnode.Id);

        foreach (var id in subnodesToDelete) await subnodes.Delete(username, id.ToString());

        // Update password if it has been changed
        if (!await users.CheckPasswordAsync(user, details.Password))
        {
          // "Invalid" password means we should reset it
          var resetToken = await users.GeneratePasswordResetTokenAsync(user);

          var result = await users.ResetPasswordAsync(user, resetToken, details.Password);
          if (!result.Succeeded)
          {
            throw new InvalidOperationException($"User password reset failed with errors for {username}.");
          }
        }

        user.IsDeclared = true;
        await users.UpdateAsync(user);
      }

      if (shouldCreate)
      {
        // Create the new user
        user = new RelayUser()
        {
          UserName = username,
          IsDeclared = true
        };

        var result = await users.CreateAsync(user, details.Password);
        if (!result.Succeeded)
        {
          throw new InvalidOperationException($"User creation failed with errors for {username}.");
        }
      }

      // Create subnodes against the user if they aren't already there
      foreach (var id in details.SubNodes)
      {
        try
        {

          if (user!.SubNodes.Select(x => x.Id).Contains(id)) continue;

          await subnodes.Create(user!, id); // TODO: bulk create instead of using the service method?
        }
        catch (Exception e) when (e is InvalidOperationException || e is ArgumentException)
        {
          throw new InvalidOperationException(
            $"The specified SubNode for this user could not be added, probably due to clashing with an existing Subnode Id: {id}", e);
        }
      }

      // capture any outstanding updates
      await db.SaveChangesAsync();
    }
  }
}
