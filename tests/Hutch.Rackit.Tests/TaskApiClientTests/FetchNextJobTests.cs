using System.Net;
using System.Text.Json;
using Flurl;
using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RichardSzalay.MockHttp;

namespace Hutch.Rackit.Tests.TaskApiClientTests;

public class FetchQueryTests
{
  private static readonly ApiClientOptions _configuredOptions = new()
  {
    BaseUrl = "http://example.com",
    CollectionId = "collection-001",
    Username = "api-user",
    Password = "abc123"
  };

  private static readonly string _fetchQueryEndpoint =
    "link_connector_api/task/nextjob";

  private readonly ILogger<TaskApiClient> _logger = Mock.Of<ILogger<TaskApiClient>>();

  [Theory]
  [InlineData(".a")] // Availability Jobs
  [InlineData(".b")] // Collection Analysis Jobs
  public async Task ReturnsNullIfNoJobs(string jobTypeSuffix)
  {
    var http = new MockHttpMessageHandler();

    http.When(
      Url.Combine(_configuredOptions.BaseUrl,
      _fetchQueryEndpoint,
      _configuredOptions.CollectionId + jobTypeSuffix)
      )
      .Respond(HttpStatusCode.NoContent);

    var client = new TaskApiClient(
      http.ToHttpClient(),
      Options.Create(_configuredOptions),
      _logger);

    TaskApiBaseResponse? result = jobTypeSuffix switch
    {
      ".a" => await client.FetchNextJobAsync<AvailabilityJob>(),
      ".b" => await client.FetchNextJobAsync<CollectionAnalysisJob>(),
      _ => null
    };

    Assert.Null(result);
  }

  [Theory]
  [InlineData(".a")] // Availability Jobs
  [InlineData(".b")] // Collection Analysis Jobs
  public async Task UsesServiceConfiguredOptionsByDefault(string jobTypeSuffix)
  {
    var http = new MockHttpMessageHandler();

    // This mock will only respond correctly if all the correct configuration is used
    // This also tests that basic auth credentials are being passed
    http.When(
      Url.Combine(_configuredOptions.BaseUrl,
      _fetchQueryEndpoint,
      _configuredOptions.CollectionId + jobTypeSuffix)
      )
      .WithHeaders("Authorization",
        "Basic " +
        TaskApiClient.EncodeCredentialsForBasicAuth(
          _configuredOptions.Username!,
          _configuredOptions.Password!))
      .Respond(HttpStatusCode.NoContent);

    var client = new TaskApiClient(
      http.ToHttpClient(),
      Options.Create(_configuredOptions),
      _logger);

    TaskApiBaseResponse? result = jobTypeSuffix switch
    {
      ".a" => await client.FetchNextJobAsync<AvailabilityJob>(),
      ".b" => await client.FetchNextJobAsync<CollectionAnalysisJob>(),
      _ => null
    };

    // The assertion is kind of irrelevant; we want to get here without exceptions
    // but we may as well confirm that the success behaviour is as expected
    Assert.Null(result);
  }

  [Theory]
  [InlineData(".a")] // Availability Jobs
  [InlineData(".b")] // Collection Analysis Jobs
  public async Task UsesOverrideOptions(string jobTypeSuffix)
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
      _fetchQueryEndpoint,
      overrideOptions.CollectionId + jobTypeSuffix)
      )
      .WithHeaders("Authorization",
        "Basic " +
        TaskApiClient.EncodeCredentialsForBasicAuth(
          overrideOptions.Username!,
          overrideOptions.Password!))
      .Respond(HttpStatusCode.NoContent);

    var client = new TaskApiClient(
      http.ToHttpClient(),
      Options.Create(_configuredOptions),
      _logger);

    TaskApiBaseResponse? result = jobTypeSuffix switch
    {
      ".a" => await client.FetchNextJobAsync<AvailabilityJob>(overrideOptions),
      ".b" => await client.FetchNextJobAsync<CollectionAnalysisJob>(overrideOptions),
      _ => null
    };

    // The assertion is kind of irrelevant; we want to get here without exceptions
    // but we may as well confirm that the success behaviour is as expected
    Assert.Null(result);
  }

  // FetchQuery returns requested response type
  [Fact]
  public async Task AvailabilityQueryReturnsAvailabilityQuery()
  {
    var response = new AvailabilityJob
    {
      Collection = _configuredOptions.CollectionId!,
      Uuid = "86",
      Owner = "user1",
      Cohort = new()
      {
        Combinator = "AND",
        Groups = []
      }
    };

    var http = new MockHttpMessageHandler();

    http.When(
      Url.Combine(_configuredOptions.BaseUrl,
      _fetchQueryEndpoint,
      _configuredOptions.CollectionId + ".a")
      )
      .Respond("application/json", JsonSerializer.Serialize(response));

    var client = new TaskApiClient(
      http.ToHttpClient(),
      Options.Create(_configuredOptions),
      _logger);

    var result = await client.FetchNextJobAsync<AvailabilityJob>();

    Assert.IsType<AvailabilityJob>(result);
  }
  [Fact]
  public async Task DistributionQueryReturnsDistributionQuery()
  {
    var response = new CollectionAnalysisJob
    {
      Analysis = "DISTRIBUTION",
      Code = "GENERIC",
      Collection = _configuredOptions.CollectionId!,
      Uuid = "86",
      Owner = "user1",
    };

    var http = new MockHttpMessageHandler();

    http.When(
      Url.Combine(_configuredOptions.BaseUrl,
      _fetchQueryEndpoint,
      _configuredOptions.CollectionId + ".b")
      )
      .Respond("application/json", JsonSerializer.Serialize(response));

    var client = new TaskApiClient(
      http.ToHttpClient(),
      Options.Create(_configuredOptions),
      _logger);

    var result = await client.FetchNextJobAsync<CollectionAnalysisJob>();

    Assert.IsType<CollectionAnalysisJob>(result);
  }
}
