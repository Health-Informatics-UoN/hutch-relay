using Hutch.Relay.Config;
using Microsoft.Extensions.Configuration;
using Hutch.Relay.Config.Helpers;
using Xunit;

namespace Hutch.Relay.Tests.HelperTests;

public class ConfigurationExtensionsTests
{
  [Theory]
  [InlineData("true", true)]
  [InlineData(null, true)]
  [InlineData("false", false)]
  public void IsEnabled_ForUpstreamTaskApi_CorrectlyDeterminesEnabledState(string? setting, bool expected)
  {
    ConfigurationManager config = new();

    if (setting is not null)
      config.AddInMemoryCollection(new Dictionary<string, string?>()
      {
        ["UpstreamTaskApi:Enable"] = setting
      });

    var actual = config.IsEnabled<TaskApiPollingOptions>();
    
    Assert.Equal(expected, actual);
  }

  [Theory]
  [InlineData("true", true)]
  [InlineData(null, false)]
  [InlineData("false", false)]
  public void IsEnabled_ForTypedQueues_CorrectlyDeterminesEnabledState(string? setting, bool expected)
  {
    ConfigurationManager config = new();

    if (setting is not null)
      config.AddInMemoryCollection(new Dictionary<string, string?>()
      {
        ["TypedQueues:Enable"] = setting
      });

    var actual = config.IsEnabled<TypedQueuesOptions>();

    Assert.Equal(expected, actual);
  }
}
