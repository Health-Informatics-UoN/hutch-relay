using System.Data.Common;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Data;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services.FilteringTermsServiceTests;

public class FilteringTermsAnyTests : IDisposable
{
  private readonly DbConnection? _connection = null;

  private static readonly IOptions<RelayBeaconOptions> _defaultOptions = Options.Create<RelayBeaconOptions>(new()
  {
    Enable = true
  });

  private readonly ApplicationDbContext _dbContext;

  public FilteringTermsAnyTests()
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
  public async Task Any_WhenBeaconDisabled_Warns(bool isBeaconEnabled)
  {
    var logger = new Mock<ILogger<FilteringTermsService>>();

    RelayBeaconOptions beaconOptions = new() { Enable = isBeaconEnabled };

    var service = new FilteringTermsService(
      logger.Object,
      Options.Create(beaconOptions),
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>(),
      _dbContext,
      Mock.Of<IRelayTaskService>());

    await service.Any();

    logger.Verify(
      x => x.Log<It.IsAnyType>( // Must use logger.Log<It.IsAnyType> to sub-out FormattedLogValues, the internal class
        LogLevel.Warning, // Match whichever log level you want here
        0, // EventId
        It.Is<It.IsAnyType>((o, t) => string.Equals(
          "GA4GH Beacon Functionality is disabled; reporting Filtering Terms cache as empty.", o.ToString())), // The type here must match the `logger.Log<T>` type used above
        null, //It.IsAny<Exception>(), // Whatever exception may have been logged with it, change as needed.
        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), // The message formatter
      isBeaconEnabled ? Times.Never : Times.Once);
  }

  [Fact]
  public async Task Any_WhenBeaconDisabled_ReturnsFalse()
  {
    RelayBeaconOptions beaconOptions = new() { Enable = false };

    var service = new FilteringTermsService(
      Mock.Of<ILogger<FilteringTermsService>>(),
      Options.Create(beaconOptions),
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>(),
      _dbContext,
      Mock.Of<IRelayTaskService>()
    );

    var actual = await service.Any();

    Assert.False(actual);
  }

  [Fact]
  public async Task List_WhenNoTerms_ReturnsFalse()
  {
    var service = new FilteringTermsService(
      Mock.Of<ILogger<FilteringTermsService>>(),
      _defaultOptions,
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>(),
      _dbContext,
      Mock.Of<IRelayTaskService>());

    var actual = await service.Any();

    Assert.False(actual);
  }

  [Fact]
  public async Task List_WhenCachedTerms_ReturnsTrue()
  {
    List<Data.Entities.FilteringTerm> cachedTerms = [
      new() {
        Term = "OMOP:123",
        SourceCategory = "Condition",
        Description = "An OMOP Condition",
      },
      new() {
        Term = "OMOP:456",
        SourceCategory = "Gender",
        Description = "Male",
        VarCat = "person"
      }
    ];

    _dbContext.AddRange(cachedTerms);
    await _dbContext.SaveChangesAsync();

    var service = new FilteringTermsService(
      Mock.Of<ILogger<FilteringTermsService>>(),
      _defaultOptions,
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>(),
      _dbContext,
      Mock.Of<IRelayTaskService>());

    var actual = await service.Any();

    Assert.True(actual);
  }
}
