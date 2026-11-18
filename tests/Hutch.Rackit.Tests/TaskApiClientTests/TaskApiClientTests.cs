using Hutch.Rackit.TaskApi;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hutch.Rackit.Tests.TaskApiClientTests;

public class TaskApiClientTests
{
  [Theory]
  [InlineData("https://taskapi.com", "https://taskapi.com/link_connector_api")]
  [InlineData("https://taskapi.com/", "https://taskapi.com/link_connector_api/")]
  [InlineData("https://taskapi.com/link_connector_api", "https://taskapi.com/link_connector_api")]
  [InlineData("https://taskapi.com/link_connector_api/", "https://taskapi.com/link_connector_api/")]
  [InlineData("https://taskapi.com/link_connector_api/link_connector_api", "https://taskapi.com/link_connector_api/link_connector_api")]
  [InlineData("https://taskapi.com/link_connector_api/link_connector_api/", "https://taskapi.com/link_connector_api/link_connector_api/")]
  [InlineData("https://taskapi.com/link_connector_api/task", "https://taskapi.com/link_connector_api/task/link_connector_api")]
  [InlineData("https://taskapi.com/link_connector_api/task/", "https://taskapi.com/link_connector_api/task/link_connector_api/")]
  public void GetBaseUrlWithRoutePrefix_ShouldReturnCorrectBaseUrl(string input, string expected)
  {
    var client = new TaskApiClient(
      new HttpClient(),
      Options.Create(new ApiClientOptions()),
      NullLogger<TaskApiClient>.Instance);

    var actual = client.GetBaseUrlWithRoutePrefix(input);

    Assert.Equal(expected, actual);
  }

  [Fact]
  public void GetBaseUrlWithRoutePrefix_WhenDisabled_ReturnsBaseUrlUnchanged()
  {
    var client = new TaskApiClient(
      new HttpClient(),
      Options.Create(new ApiClientOptions { AppendRoutePrefix = false }),
      NullLogger<TaskApiClient>.Instance);

    const string baseUrl = "https://taskapi.com/";

    Assert.Equal(baseUrl, client.GetBaseUrlWithRoutePrefix(baseUrl));
  }
}
