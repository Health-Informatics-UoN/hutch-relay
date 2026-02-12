using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Tests.Auth;

public class Helpers
{
  public static async Task<bool> CanAuthorizeUserWithPolicyAsync(ClaimsPrincipal user, AuthorizationPolicy policy, object? resource = null)
  {
    var handlers = policy.Requirements
      .Select(x => x as IAuthorizationHandler)
      .Where(x => x is not null)!
      .ToArray<IAuthorizationHandler>();
    // add your custom authorization handlers here to the `handlers` collection // TODO: parameterise?

    var authorizationOptions = Options.Create(new AuthorizationOptions());
    authorizationOptions.Value.AddPolicy(nameof(policy), policy);

    var policyProvider = new DefaultAuthorizationPolicyProvider(authorizationOptions);

    var handlerProvider = new DefaultAuthorizationHandlerProvider(handlers);

    var contextFactory = new DefaultAuthorizationHandlerContextFactory();

    var authorizationService = new DefaultAuthorizationService(
        policyProvider,
        handlerProvider,
        new NullLogger<DefaultAuthorizationService>(),
        contextFactory,
        new DefaultAuthorizationEvaluator(),
        authorizationOptions);

    var result = resource is not null
      ? await authorizationService.AuthorizeAsync(user, resource, policy)
      : await authorizationService.AuthorizeAsync(user, policy);

    return result.Succeeded;
  }

}
