using Hutch.Rackit.TaskApi;

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
    var actual = TaskApiClient.GetBaseUrlWithRoutePrefix(input);
    
    Assert.Equal(expected, actual);
  }
}
