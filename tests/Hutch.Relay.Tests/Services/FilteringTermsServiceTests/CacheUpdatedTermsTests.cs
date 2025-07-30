using System.Data.Common;
using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services.FilteringTermsServiceTests;

public class CacheUpdatedTermsTests : IDisposable
{
  private readonly DbConnection? _connection = null;

  private static readonly IOptions<RelayBeaconOptions> _defaultOptions = Options.Create<RelayBeaconOptions>(new()
  {
    Enable = true
  });

  private readonly ApplicationDbContext _dbContext;

  public CacheUpdatedTermsTests()
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
  public async Task CacheUpdatedTerms_WhenBeaconDisabled_LogsWarningAndReturns(bool isBeaconEnabled)
  {
    // Arrange
    var results = new JobResult()
    {
      Results = new QueryResult()
      {
        Files = [ new ResultFile()
        {
          FileName = ResultFileName.DemographicsDistribution,
        }]
      }
    };

    RelayBeaconOptions beaconOptions = new()
    {
      Enable = isBeaconEnabled
    };

    var logger = new Mock<ILogger<FilteringTermsService>>();

    var filteringTermsService = new FilteringTermsService(
      logger.Object,
      Options.Create(beaconOptions),
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>(),
      _dbContext,
      Mock.Of<IRelayTaskService>());

    // Act
    await filteringTermsService.CacheUpdatedTerms(results);

    // Assert
    logger.Verify(
      x => x.Log<It.IsAnyType>( // Must use logger.Log<It.IsAnyType> to sub-out FormattedLogValues, the internal class
        LogLevel.Warning, // Match whichever log level you want here
        0, // EventId
        It.Is<It.IsAnyType>((o, t) => string.Equals(
          "GA4GH Beacon Functionality is disabled; not updated Filtering Terms cache.", o.ToString())), // The type here must match the `logger.Log<T>` type used above
        null, //It.IsAny<Exception>(), // Whatever exception may have been logged with it, change as needed.
        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), // The message formatter
      isBeaconEnabled ? Times.Never : Times.Once);

    // Nothing was done to the DB
    Assert.Empty(_dbContext.FilteringTerms);
  }

  [Fact]
  public async Task CacheUpdatedTerms_WhenNoDistributionResults_LogsWarningAndReturns()
  {
    // Arrange
    var results = new JobResult()
    {
      Results = new QueryResult()
      {
        Files = [ new ResultFile()
        {
          FileName = ResultFileName.DemographicsDistribution,
        }]
      }
    };

    var logger = new Mock<ILogger<FilteringTermsService>>();

    var filteringTermsService = new FilteringTermsService(
      logger.Object,
      _defaultOptions,
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>(),
      _dbContext,
      Mock.Of<IRelayTaskService>());

    // Act
    await filteringTermsService.CacheUpdatedTerms(results);

    // Assert
    logger.Verify(
      x => x.Log<It.IsAnyType>( // Must use logger.Log<It.IsAnyType> to sub-out FormattedLogValues, the internal class
        LogLevel.Warning, // Match whichever log level you want here
        0, // EventId
        It.Is<It.IsAnyType>((o, t) => string.Equals(
          "A FilteringTerms update was attempted from a downstream result that contains no Code Distribution Results. The update will not proceed.", o.ToString())), // The type here must match the `logger.Log<T>` type used above
        null, //It.IsAny<Exception>(), // Whatever exception may have been logged with it, change as needed.
        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), // The message formatter
      Times.Once);

    // Nothing was done to the DB
    Assert.Empty(_dbContext.FilteringTerms);
  }

  [Fact]
  public async Task CacheUpdatedTerms_WhenEmptyDistributionResults_LogsWarningAndReturns()
  {
    // Arrange
    var results = new JobResult()
    {
      Results = new QueryResult()
      {
        Files = [ new ResultFile()
        {
          FileName = ResultFileName.CodeDistribution,
        }.WithData("HEADERS,ONLY,NO,RECORDS")]
      }
    };

    var logger = new Mock<ILogger<FilteringTermsService>>();

    var filteringTermsService = new FilteringTermsService(
      logger.Object,
      _defaultOptions,
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>(),
      _dbContext,
      Mock.Of<IRelayTaskService>());

    // Act
    await filteringTermsService.CacheUpdatedTerms(results);

    // Assert
    logger.Verify(
      x => x.Log<It.IsAnyType>( // Must use logger.Log<It.IsAnyType> to sub-out FormattedLogValues, the internal class
        LogLevel.Warning, // Match whichever log level you want here
        0, // EventId
        It.Is<It.IsAnyType>((o, t) => string.Equals(
          "A FilteringTerms update was attempted from a downstream result that contains no Code Distribution Results. The update will not proceed.", o.ToString())), // The type here must match the `logger.Log<T>` type used above
        null, //It.IsAny<Exception>(), // Whatever exception may have been logged with it, change as needed.
        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), // The message formatter
      Times.Once);

    // Nothing was done to the DB
    Assert.Empty(_dbContext.FilteringTerms);
  }

  [Fact]
  public async Task CacheUpdatedTerms_WhenDistributionResults_ReplacesData()
  {
    // Arrange
    List<FilteringTerm> oldData = [
      new() {
        Term = "OMOP:123",
        SourceCategory = CodeCategory.Observation
      },
      new() {
        Term = "OMOP:456",
        SourceCategory = CodeCategory.Measurement
      }
    ];
    _dbContext.AddRange(oldData);
    await _dbContext.SaveChangesAsync();


    var results = new JobResult()
    {
      Results = new QueryResult()
      {
        Files = [ new ResultFile()
        {
          FileName = ResultFileName.CodeDistribution,
        }
        .WithData(new List<GenericDistributionRecord>() {
          new() {
            Code = "OMOP:987",
            Collection = "",
            Category = "Gender",
            OmopDescription = "Male"
          },
          new() {
            Code = "OMOP:654",
            Collection = "",
            Category = "Condition",
            OmopDescription = "Cancer"
          },
          new() {
            Code = "OMOP:321",
            Collection = "",
            Category = "Race",
            OmopDescription = "Human"
          }
        })]
      }
    };

    List<FilteringTerm> expected = [
      new() {
        Term = "OMOP:987",
        SourceCategory = "Gender",
        Description = "Male",
        VarCat = "person"
      },
      new() {
        Term = "OMOP:654",
        SourceCategory = "Condition",
        Description = "Cancer",
      },
      new() {
        Term = "OMOP:321",
        SourceCategory = "Race",
        Description = "Human",
        VarCat = "person"
      }
    ];

    var logger = new Mock<ILogger<FilteringTermsService>>();

    var filteringTermsService = new FilteringTermsService(
      logger.Object,
      _defaultOptions,
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>(),
      _dbContext,
      Mock.Of<IRelayTaskService>());

    // Act
    await filteringTermsService.CacheUpdatedTerms(results);

    // Assert
    Assert.Equivalent(expected, _dbContext.FilteringTerms);
  }
}
