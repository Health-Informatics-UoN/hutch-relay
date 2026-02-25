using Xunit;
using Hutch.Relay.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;

namespace Hutch.Relay.Tests.Auth.AuthPolicies;

public class IsTaskApiCollectionOwnerTests
  : IClassFixture<UserCollectionsFixture>
{
  private readonly UserCollectionsFixture _fixture;
  private readonly UserManager<RelayUser> _userManager;


  public IsTaskApiCollectionOwnerTests(UserCollectionsFixture fixture)
  {
    _fixture = fixture;

    _userManager = MockHelpers.TestUserManager(new UserStore<RelayUser>(_fixture.DbContext));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("INVALID_FORMAT")]
  [InlineData("__SUBNODE1")]
  [InlineData("__SUBNODE2")]
  public async Task IsTaskApiCollectionOwner_WhenUserCollectionValid_Authorized(string? collectionId)
  {
    var user = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.Name, UserCollectionsFixture.User1.username)], "Basic"));

    var services = new ServiceCollection();
    services.AddScoped((s) => _fixture.DbContext);

    var httpContext = new DefaultHttpContext
    {
      RequestServices = services.BuildServiceProvider(),
      User = user
    };

    if (collectionId is not null)
      httpContext.Request.RouteValues = new RouteValueDictionary
      {
        ["collectionId"] = collectionId switch
        {
          "__SUBNODE1" => UserCollectionsFixture.SubNode1.ToString(),
          "__SUBNODE2" => UserCollectionsFixture.SubNode2.ToString(),
          _ => collectionId
        }
      };

    var result = await Helpers.CanAuthorizeUserWithPolicyAsync(user, Relay.Auth.AuthPolicies.IsTaskApiCollectionOwner, httpContext);

    if (collectionId == "__SUBNODE1") Assert.True(result);
    else Assert.False(result);
  }
}
