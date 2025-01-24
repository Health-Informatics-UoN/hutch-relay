using System.Net;
using Flurl;
using Hutch.Rackit.TaskApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RichardSzalay.MockHttp;

namespace Hutch.Rackit.Tests.TaskApiClientTests;

public class SubmitResultTests
{
  private static readonly ApiClientOptions _configuredOptions = new()
  {
    BaseUrl = "http://example.com",
    CollectionId = "collection-001",
    Username = "api-user",
    Password = "abc123"
  };

  private static readonly string _submitResultEndpoint =
    "link_connector_api/task/result";

  private static readonly string _jobId = "a030666b-2aed-4657-a126-498355ce89c4";

  private readonly ILogger<TaskApiClient> _logger = Mock.Of<ILogger<TaskApiClient>>();

  [Fact]
  public async Task NonSuccessStatusCode_Throws()
  {
    var http = new MockHttpMessageHandler();

    http.When(
      Url.Combine(_configuredOptions.BaseUrl,
      _submitResultEndpoint,
      _jobId,
      _configuredOptions.CollectionId)
      )
      .Respond(HttpStatusCode.InternalServerError);

    var client = new TaskApiClient(
      http.ToHttpClient(),
      Options.Create(_configuredOptions),
      _logger);

    await Assert.ThrowsAsync<RackitApiClientException>(() => client.SubmitResultAsync(_jobId, new()));
  }

  [Fact]
  public async Task Unexpected2xxBody_Throws()
  {
    var http = new MockHttpMessageHandler();

    http.When(
      Url.Combine(_configuredOptions.BaseUrl,
      _submitResultEndpoint,
      _jobId,
      _configuredOptions.CollectionId)
      )
      .Respond(HttpStatusCode.OK, new StringContent("unexpected response body"));

    var client = new TaskApiClient(
      http.ToHttpClient(),
      Options.Create(_configuredOptions),
      _logger);

    await Assert.ThrowsAsync<RackitApiClientException>(() => client.SubmitResultAsync(_jobId, new()));
  }

  [Fact]
  public async Task ExpectedSuccessResponse_ReturnsSuccessfully()
  {
    var http = new MockHttpMessageHandler();

    http.When(
      Url.Combine(_configuredOptions.BaseUrl,
      _submitResultEndpoint,
      _jobId,
      _configuredOptions.CollectionId)
      )
      .Respond(HttpStatusCode.OK, new StringContent("Job saved"));

    var client = new TaskApiClient(
      http.ToHttpClient(),
      Options.Create(_configuredOptions),
      _logger);

    await client.SubmitResultAsync(_jobId, new());

    // Implicit assertion that nothing throws
  }

  [Fact]
  public async Task UsesServiceConfiguredOptionsByDefault()
  {
    var http = new MockHttpMessageHandler();

    // This mock will only respond correctly if all the correct configuration is used
    // This also tests that basic auth credentials are being passed
    http.When(
      Url.Combine(_configuredOptions.BaseUrl,
      _submitResultEndpoint,
      _jobId,
      _configuredOptions.CollectionId)
      )
      .WithHeaders("Authorization",
        "Basic " +
        TaskApiClient.EncodeCredentialsForBasicAuth(
          _configuredOptions.Username!,
          _configuredOptions.Password!))
      .Respond(HttpStatusCode.OK, new StringContent("Job saved"));


    var client = new TaskApiClient(
      http.ToHttpClient(),
      Options.Create(_configuredOptions),
      _logger);

    await client.SubmitResultAsync(_jobId, new());

    // Implicit assertion that nothing throws
  }

  [Fact]
  public async Task UsesOverrideOptions()
  {
    var http = new MockHttpMessageHandler();

    var overrideOptions = new ApiClientOptions
    {
      BaseUrl = "https://api.different.com",
      CollectionId = "collection-xyz",
      Username = "override",
      Password = "xyz789",
    };

    // This mock will only respond correctly if all the correct configuration is used
    http.When(
      Url.Combine(overrideOptions.BaseUrl,
      _submitResultEndpoint,
      _jobId,
      overrideOptions.CollectionId)
      )
      .WithHeaders("Authorization",
        "Basic " +
        TaskApiClient.EncodeCredentialsForBasicAuth(
          overrideOptions.Username!,
          overrideOptions.Password!))
      .Respond(HttpStatusCode.OK, new StringContent("Job saved"));

    var client = new TaskApiClient(
      http.ToHttpClient(),
      Options.Create(_configuredOptions),
      _logger);

    await client.SubmitResultAsync(_jobId, new(), overrideOptions);

    // Implicit assertion that nothing throws
  }
}
