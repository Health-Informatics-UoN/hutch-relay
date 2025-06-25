using Hutch.Relay.Config;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
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
  SubNodeService subnodes) // TODO: fix ISubNodeService usage
{
  private readonly DownstreamUsersOptions _downstreamUsers = downstreamUsersOptions.Value;

  public async Task ReconcileDownstreamUsers()
  {
    var existingUsers = await db.RelayUsers.AsNoTracking().ToListAsync();

    // normalise our declared usernames the same way aspnet identity does
    var normalisedDeclaredUsernames = _downstreamUsers.DownstreamUsers.Keys
      .Select(x => normalizer.NormalizeName(x));

    // Start by deleting undeclared users, which will cascade to delete subnodes too
    foreach (var existingUser in existingUsers)
    {

      // not sure how this can practically happen,
      // but if they don't have a username we can't username match them anyway
      if (existingUser.NormalizedUserName is null) continue;


      if (!normalisedDeclaredUsernames.Contains(existingUser.NormalizedUserName))
      {
        await users.DeleteAsync(existingUser); // TODO: does this work? We didn't fetch the user via UserManager...
      }
    }

    // Then foreach declared user, add them if they don't exist, or update them (including subnodes) if they do
    foreach (var (username, details) in _downstreamUsers.DownstreamUsers)
    {
      // Prepare the configured subnodes collection
      details.SubNodes ??= [];
      if (details.SubNode is not null)
        details.SubNodes.Add(details.SubNode.Value); // Merge single subnode if applicable

      var user = await users.FindByNameAsync(username);
      var shouldCreate = user is null;

      if (!shouldCreate)
      {
        // Terminate if this user already exists imperatively!
        if (!user!.IsDeclared)
          throw new InvalidOperationException(
            $"A Downstream User declared in config already exists from being added manually. Please change the config or manually remove the user '{username}'");

        // Delete subnodes we don't want anymore
        foreach (var subnode in user.SubNodes) // will subnodes be populated here?
          if (!details.SubNodes!.Contains(subnode.Id))
            await subnodes.Delete(username, subnode.Id.ToString());

        // Update user details
        await users.SetUserNameAsync(user, username);

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
        if (user!.SubNodes.Select(x => x.Id).Contains(id)) continue;

        await subnodes.Create(user!, id);
      }
    }
  }
}
