using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services.IndividualsQueryServiceTests;

public class GetResultsSummaryTests
{
  [Theory]
  [InlineData(1, Granularity.boolean)]
  [InlineData(71465, Granularity.count)]
  public void GetResultsSummary_WhenNonZeroCount_ReturnsWithCorrectGranularity(int count, Granularity defaultGranularity)
  {
    var options = Options.Create<RelayBeaconOptions>(new()
    {
      Enable = false,
      SecurityAttributes =
      {
        DefaultGranularity = defaultGranularity
      }
    });

    var service = new IndividualsQueryService(
      Mock.Of<ILogger<IndividualsQueryService>>(),
      options,
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>());

    var actual = service.GetResultsSummary(count);

    Assert.True(actual.Exists);

    switch (defaultGranularity)
    {
      case Granularity.boolean:
        Assert.Null(actual.NumTotalResults);
        break;
      default:
        Assert.Equal(count, actual.NumTotalResults);
        break;
    }
  }
  
  [Theory]
  [InlineData(Granularity.boolean)]
  [InlineData(Granularity.count)]
  public void GetResultsSummary_WhenZeroCount_ReturnsEmptyWithCorrectGranularity(Granularity defaultGranularity)
  {
    var options = Options.Create<RelayBeaconOptions>(new()
    {
      Enable = false,
      SecurityAttributes =
      {
        DefaultGranularity = defaultGranularity
      }
    });

    var service = new IndividualsQueryService(
      Mock.Of<ILogger<IndividualsQueryService>>(),
      options,
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>());

    var actual = service.GetResultsSummary(0);

    Assert.False(actual.Exists);

    switch (defaultGranularity)
    {
      case Granularity.boolean:
        Assert.Null(actual.NumTotalResults);
        break;
      default:
        Assert.Equal(0, actual.NumTotalResults);
        break;
    }
  }
  
  [Theory]
  [InlineData(Granularity.boolean)]
  [InlineData(Granularity.count)]
  public void GetEmptySummary_ReturnsEmptyWithCorrectGranularity(Granularity defaultGranularity)
  {
    var options = Options.Create<RelayBeaconOptions>(new()
    {
      Enable = false,
      SecurityAttributes =
      {
        DefaultGranularity = defaultGranularity
      }
    });

    var service = new IndividualsQueryService(
      Mock.Of<ILogger<IndividualsQueryService>>(),
      options,
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>());

    var actual = service.GetEmptySummary();

    Assert.False(actual.Exists);

    switch (defaultGranularity)
    {
      case Granularity.boolean:
        Assert.Null(actual.NumTotalResults);
        break;
      default:
        Assert.Equal(0, actual.NumTotalResults);
        break;
    }
  }
}
