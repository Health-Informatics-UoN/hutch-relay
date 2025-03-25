using Xunit;
using Hutch.Relay.Auth.Basic;
using Hutch.Relay.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Hutch.Relay.Tests.Auth;

public class BasicAuthHandlerTests
  : IClassFixture<UserCollectionsFixture>
{
  private readonly UserCollectionsFixture _fixture;
  private readonly IOptionsMonitor<BasicAuthSchemeOptions> _options;
  private readonly UserManager<RelayUser> _userManager;


  public BasicAuthHandlerTests(UserCollectionsFixture fixture)
  {
    _fixture = fixture;

    var options = new Mock<IOptionsMonitor<BasicAuthSchemeOptions>>();
    options
      .Setup(x => x.Get(It.IsAny<string>()))
      .Returns(new BasicAuthSchemeOptions() { Realm = "test" });
    _options = options.Object;

    _userManager = MockHelpers.TestUserManager(new UserStore<RelayUser>(_fixture.Database));
  }

  [Fact]
  public async Task HandleAuthenticateAsync_MissingBasicAuthHeader_ReturnsFailResult()
  {
    var handler = new BasicAuthHandler(
      _options,
      new NullLoggerFactory(),
      new UrlTestEncoder(),
      _fixture.Database,
      _userManager
    );

    var context = new DefaultHttpContext();

    await handler.InitializeAsync(
      new(BasicAuthDefaults.AuthenticationScheme, null, typeof(BasicAuthHandler)),
      context);
    var result = await handler.AuthenticateAsync();

    Assert.False(result.Succeeded);
  }

  [Fact]
  public async Task HandleAuthenticateAsync_WrongScheme_ReturnsFailResult()
  {
    var handler = new BasicAuthHandler(
      _options,
      new NullLoggerFactory(),
      new UrlTestEncoder(),
      _fixture.Database,
      _userManager
    );

    var context = new DefaultHttpContext();
    context.Request.Headers.Authorization = "Bearer dummy-value";

    await handler.InitializeAsync(
      new(BasicAuthDefaults.AuthenticationScheme, null, typeof(BasicAuthHandler)),
      context);
    var result = await handler.AuthenticateAsync();

    Assert.False(result.Succeeded);
  }

  [Fact]
  public async Task HandleAuthenticateAsync_InvalidCredentialsFormat_ReturnsFailResult()
  {
    var handler = new BasicAuthHandler(
      _options,
      new NullLoggerFactory(),
      new UrlTestEncoder(),
      _fixture.Database,
      _userManager
    );

    var context = new DefaultHttpContext();
    context.Request.Headers.Authorization = BasicAuthDefaults.AuthenticationScheme + " invalid-credentials-format";

    await handler.InitializeAsync(
      new(BasicAuthDefaults.AuthenticationScheme, null, typeof(BasicAuthHandler)),
      context);
    var result = await handler.AuthenticateAsync();

    Assert.False(result.Succeeded);
  }

  [Fact]
  public async Task HandleAuthenticateAsync_IncorrectUserCredentials_ReturnsFailResult()
  {
    var handler = new BasicAuthHandler(
      _options,
      new NullLoggerFactory(),
      new UrlTestEncoder(),
      _fixture.Database,
      _userManager
    );

    var incorrectCredentials = "non-existent-username:password"u8.ToArray();
    var b64Credentials = Convert.ToBase64String(incorrectCredentials);

    var context = new DefaultHttpContext();
    context.Request.Headers.Authorization = $"{BasicAuthDefaults.AuthenticationScheme} {b64Credentials}";

    await handler.InitializeAsync(
      new(BasicAuthDefaults.AuthenticationScheme, null, typeof(BasicAuthHandler)),
      context);
    var result = await handler.AuthenticateAsync();

    Assert.False(result.Succeeded);
  }

  [Fact]
  public async Task HandleAuthenticateAsync_IncorrectPassword_ReturnsFailResult()
  {
    var handler = new BasicAuthHandler(
      _options,
      new NullLoggerFactory(),
      new UrlTestEncoder(),
      _fixture.Database,
      _userManager
    );

    // mix up user 1 and user 2's username and password <3
    var incorrectCredentials =
      System.Text.Encoding.UTF8.GetBytes(
        $"{UserCollectionsFixture.User1.username}:{UserCollectionsFixture.User2.password}");
    var b64Credentials = Convert.ToBase64String(incorrectCredentials);

    var context = new DefaultHttpContext();
    context.Request.Headers.Authorization = $"{BasicAuthDefaults.AuthenticationScheme} {b64Credentials}";

    await handler.InitializeAsync(
      new(BasicAuthDefaults.AuthenticationScheme, null, typeof(BasicAuthHandler)),
      context);
    var result = await handler.AuthenticateAsync();

    Assert.False(result.Succeeded);
  }

  [Fact]
  public async Task HandleAuthenticateAsync_InvalidUserCollection_ReturnsFailResult()
  {
    var handler = new BasicAuthHandler(
      _options,
      new NullLoggerFactory(),
      new UrlTestEncoder(),
      _fixture.Database,
      _userManager
    );

    var correctCredentials =
      System.Text.Encoding.UTF8.GetBytes(
        $"{UserCollectionsFixture.User1.username}:{UserCollectionsFixture.User1.password}");
    var b64Credentials = Convert.ToBase64String(correctCredentials);

    var context = new DefaultHttpContext();
    context.Request.Headers.Authorization = $"{BasicAuthDefaults.AuthenticationScheme} {b64Credentials}";

    // Use User 2's Sub Node with User 1's credentials
    context.Request.RouteValues.Add("collectionId", UserCollectionsFixture.SubNode2.ToString());

    await handler.InitializeAsync(
      new(BasicAuthDefaults.AuthenticationScheme, null, typeof(BasicAuthHandler)),
      context);
    var result = await handler.AuthenticateAsync();

    Assert.False(result.Succeeded);
  }

  [Theory]
  [MemberData(nameof(UserCollectionsFixture.GetUserCollections), MemberType = typeof(UserCollectionsFixture))]
  public async Task HandleAuthenticateAsync_ValidUserCollection_ReturnsSuccessResult(
    string username, string password, string collectionId)
  {
    var handler = new BasicAuthHandler(
      _options,
      new NullLoggerFactory(),
      new UrlTestEncoder(),
      _fixture.Database,
      _userManager
    );

    var correctCredentials =
      System.Text.Encoding.UTF8.GetBytes(
        $"{username}:{password}");
    var b64Credentials = Convert.ToBase64String(correctCredentials);

    var context = new DefaultHttpContext();
    context.Request.Headers.Authorization = $"{BasicAuthDefaults.AuthenticationScheme} {b64Credentials}";
    context.Request.RouteValues.Add("collectionId", collectionId);

    await handler.InitializeAsync(
      new(BasicAuthDefaults.AuthenticationScheme, null, typeof(BasicAuthHandler)),
      context);
    var result = await handler.AuthenticateAsync();

    Assert.True(result.Succeeded);
  }
}
