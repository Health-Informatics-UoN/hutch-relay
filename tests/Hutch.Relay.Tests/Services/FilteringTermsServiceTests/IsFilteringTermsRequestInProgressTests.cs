using System.Data.Common;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Constants;
using Hutch.Relay.Data;
using Hutch.Relay.Models;
using Hutch.Relay.Models.Beacon;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services.FilteringTermsServiceTests;

public class IsFilteringTermsRequestInProgressTests : IDisposable
{
  private readonly DbConnection? _connection = null;

  private static readonly IOptions<RelayBeaconOptions> _defaultOptions = Options.Create<RelayBeaconOptions>(new()
  {
    Enable = true
  });

  private readonly ApplicationDbContext _dbContext;

  public IsFilteringTermsRequestInProgressTests()
  {
    // Ensure a unique DB per Test
    _dbContext = FixtureHelpers.NewDbContext(ref _connection);
    _dbContext.Database.EnsureCreated();
  }

  public void Dispose()
  {
    _dbContext.Database.EnsureDeleted();
    _connection?.Dispose();
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public async Task IsFilteringTermsRequestInProgressTests_ReturnsCorrectly(bool expected)
  {
    var relayTasks = new Mock<IRelayTaskService>();
    relayTasks.Setup(x => x.ListIncomplete())
      .Returns(() => Task.FromResult(expected
      ? new List<RelayTaskModel>()
        {
          new() { Collection = "", Id = "123", Type = TaskTypes.TaskApi_CodeDistribution }
        }.AsEnumerable()
      : []));

    var service = new FilteringTermsService(
      Mock.Of<ILogger<FilteringTermsService>>(),
      _defaultOptions,
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>(),
      _dbContext,
      relayTasks.Object);

    // Act
    var actual = await service.IsFilteringTermsRequestInProgress();

    // Assert
    Assert.Equal(expected, actual);
  }
}
