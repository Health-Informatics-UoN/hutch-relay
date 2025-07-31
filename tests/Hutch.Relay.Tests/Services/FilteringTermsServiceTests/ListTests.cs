using System.Data.Common;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Data;
using Hutch.Relay.Models.Beacon;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services.FilteringTermsServiceTests;

public class ListTests : IDisposable
{
  private readonly DbConnection? _connection = null;

  private static readonly IOptions<RelayBeaconOptions> _defaultOptions = Options.Create<RelayBeaconOptions>(new()
  {
    Enable = true
  });

  private readonly ApplicationDbContext _dbContext;

  public ListTests()
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
  public async Task List_WhenBeaconDisabled_Warns(bool isBeaconEnabled)
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

    await service.List();

    logger.Verify(
      x => x.Log<It.IsAnyType>( // Must use logger.Log<It.IsAnyType> to sub-out FormattedLogValues, the internal class
        LogLevel.Warning, // Match whichever log level you want here
        0, // EventId
        It.Is<It.IsAnyType>((o, t) => string.Equals(
          "GA4GH Beacon Functionality is disabled; returning empty Filtering Terms list", o.ToString())), // The type here must match the `logger.Log<T>` type used above
        null, //It.IsAny<Exception>(), // Whatever exception may have been logged with it, change as needed.
        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), // The message formatter
      isBeaconEnabled ? Times.Never : Times.Once);
  }

  [Fact]
  public async Task List_WhenBeaconDisabled_ReturnsEmpty()
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

    var actual = await service.List();

    Assert.Empty(actual);
  }

  [Fact]
  public async Task List_WhenCachedTerms_ReturnsBeaconFilteringTermsResponse()
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

    List<FilteringTerm> expected = [
      new() {
        Id = "OMOP:123",
        Label = "An OMOP Condition"
      },
      new() {
        Id = "OMOP:456",
        Label = "Male"
      }
    ];

    var service = new FilteringTermsService(
      Mock.Of<ILogger<FilteringTermsService>>(),
      _defaultOptions,
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>(),
      _dbContext,
      Mock.Of<IRelayTaskService>());

    var actual = await service.List();

    Assert.Equivalent(expected, actual);
  }

  [Theory]
  [InlineData(11, null, 10)] // default to 10
  [InlineData(11, 10, 10)]
  [InlineData(2, 1, 1)]
  [InlineData(11, -10, 10)] // negative reverts to default
  [InlineData(2, 10, 2)] // fewer records than limit
  [InlineData(11, 0, 11)] // limit 0 means unlimited
  public async Task List_Limit(int nTerms, int? limit, int expectedCount)
  {
    List<Data.Entities.FilteringTerm> cachedTerms = [.. Enumerable.Range(0, nTerms)
      .Select(i => new Data.Entities.FilteringTerm() {
        Term = $"OMOP:123{i}",
        SourceCategory = "Condition",
        Description = "An OMOP Condition",
      })];

    _dbContext.AddRange(cachedTerms);
    await _dbContext.SaveChangesAsync();

    var service = new FilteringTermsService(
      Mock.Of<ILogger<FilteringTermsService>>(),
      _defaultOptions,
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>(),
      _dbContext,
      Mock.Of<IRelayTaskService>());

    var actual = limit is null
      ? await service.List(0)
      : await service.List(0, limit.Value);

    Assert.Equal(expectedCount, actual.Count);
  }

  [Theory]
  [InlineData(11, null, 10, 0)] // default to 0
  [InlineData(11, 1, 10, 10)] // skip a whole page, sized by limit
  [InlineData(11, 2, 1, 2)]
  [InlineData(11, -10, 5, 0)] // negative reverts to default
  [InlineData(6, 1, 10, null)] // fewer records than limit; returns empty because skip takes it beyond
  [InlineData(6, 4, 2, null)] // fewer records than pages skipped; returns empty because skip takes it beyond
  public async Task List_Skip(int nTerms, int? skip, int limit, int? expectedStartIndex)
  {
    List<Data.Entities.FilteringTerm> cachedTerms = [.. Enumerable.Range(0, nTerms)
      .Select(i => new Data.Entities.FilteringTerm() {
        Term = $"OMOP:123{i}",
        SourceCategory = "Condition",
        Description = "An OMOP Condition",
      })];

    // order of insertion is important to this test :(
    foreach (var entity in cachedTerms)
    {
      _dbContext.Add(entity);
      await _dbContext.SaveChangesAsync();
    }

    var service = new FilteringTermsService(
      Mock.Of<ILogger<FilteringTermsService>>(),
      _defaultOptions,
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>(),
      _dbContext,
      Mock.Of<IRelayTaskService>());

    var actual = skip is null
      ? await service.List()
      : await service.List(skip.Value, limit);

    if (expectedStartIndex is not null)
    {
      var expectedCachedTerm = cachedTerms[expectedStartIndex.Value];

      Assert.Equivalent(
        new FilteringTerm()
        {
          Id = expectedCachedTerm.Term,
          Label = expectedCachedTerm.Description
        },
        actual[0]);
    }
    else
    {
      Assert.Empty(actual);
    }
  }
}
