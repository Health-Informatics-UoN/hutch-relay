using Hutch.Relay.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Hutch.Relay.Auth;

public class AuthPolicies
{
  public static AuthorizationPolicy IsAuthenticated // TODO: maybe differentiate schemes or realms later for Task API vs Beacon?
    => new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

  /// <summary>
  /// Checks that the authenticated user is the owner of a subnode (Collection)
  /// when `collectionId` provided in a route
  /// </summary>
  /// <returns></returns>
  public static AuthorizationPolicy IsTaskApiCollectionOwner // TODO: in future also use role claims to separate Task API users from Beacon?
    => new AuthorizationPolicyBuilder()
        .Combine(IsAuthenticated)
        .RequireAssertion(context =>
        {
          // resolve services and resources
          var httpContext = (DefaultHttpContext?)context.Resource;

          var db = httpContext?.RequestServices.GetService<ApplicationDbContext>();
          if (db is null) return false;

          var logger = httpContext?.RequestServices.GetService<ILogger<AuthPolicies>>();

          // Get request `collectionId` from route
          var routeCollectionId = (string?)httpContext?.Request.RouteValues.GetValueOrDefault("collectionId");
          if (routeCollectionId is null) return false;

          var separatorIndex = routeCollectionId.IndexOf('.');

          var collectionId = separatorIndex >= 0
            ? routeCollectionId[..separatorIndex]
            : routeCollectionId;

          if (!Guid.TryParse(collectionId, out var routeCollectionGuid))
          {
            logger?.LogWarning(
              "Could not parse collectionId: {CollectionId}",
              collectionId);
            return false;
          }
          // Check if the collection ID matches a SubNode for this user
          var username = context.User.Identity?.Name;
          if (username is null) return false; // should be impossible since we require an authenticated user, but if it does happen, they're unauthorised

          var clientCollections = db.SubNodes.AsNoTracking()
            .Where(subNode =>
              subNode.RelayUsers.Select(user => user.UserName)
                .Contains(username))
            .Select(x => x.Id)
            .ToList();

          if (!clientCollections.Contains(routeCollectionGuid))
          {
            logger?.LogWarning("Collection {CollectionId} is not valid for clientId {Username}", collectionId, username);
            return false;
          }

          return true;
        })
        .Build();
}
