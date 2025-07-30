using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Constants;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Models;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services;

public class FilteringTermsServiceTests
{
  private static readonly IOptions<RelayBeaconOptions> DefaultOptions = Options.Create<RelayBeaconOptions>(new()
  {
    Enable = true
  });

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public async Task RequestUpdatedTerms_BeaconDisabled_LogsWarningAndDoesNothing(bool isBeaconEnabled)
  {
    var logger = new Mock<ILogger<FilteringTermsService>>();

    var subNodes = new Mock<ISubNodeService>();
    var subnodes = new List<SubNodeModel>([new()
      {
        Id = Guid.NewGuid(),
        Owner = "user"
      }]);
    subNodes.Setup(x => x.List()).Returns(Task.FromResult(subnodes.AsEnumerable()));

    var downstreamTasks = new Mock<IDownstreamTaskService>();
    downstreamTasks.Setup(x => x.Enqueue(It.IsAny<AvailabilityJob>(), It.IsAny<List<SubNodeModel>>()));

    var filteringTermsService = new FilteringTermsService(
      logger.Object,
      Options.Create<RelayBeaconOptions>(new()
      {
        Enable = isBeaconEnabled
      }), subNodes.Object, downstreamTasks.Object);

    await filteringTermsService.RequestUpdatedTerms();

    logger.Verify(
      x => x.Log<It.IsAnyType>( // Must use logger.Log<It.IsAnyType> to sub-out FormattedLogValues, the internal class
        LogLevel.Warning, // Match whichever log level you want here
        0, // EventId
        It.Is<It.IsAnyType>((o, t) => string.Equals(
          "GA4GH Beacon Functionality is disabled; not requesting FilteringTerms.", o.ToString())), // The type here must match the `logger.Log<T>` type used above
        null, //It.IsAny<Exception>(), // Whatever exception may have been logged with it, change as needed.
        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), // The message formatter
      isBeaconEnabled ? Times.Never : Times.Once);

    downstreamTasks.Verify(
    x => x.Enqueue(
      It.IsAny<TaskApiBaseResponse>(),
      It.IsAny<List<SubNodeModel>>()),
    isBeaconEnabled ? Times.Once : Times.Never);
  }

  [Fact]
  public async Task RequestUpdatedTerms_EnqueuesDownstream_BeaconCodeDistributionTask()
  {
    var testPollingDuration = TimeSpan.FromSeconds(20);

    // Arrange
    var expectedTask = new CollectionAnalysisJob()
    {
      Analysis = AnalysisType.Distribution,
      Code = DistributionCode.Generic,
      Collection = RelayBeaconTaskDetails.Collection,
      Owner = RelayBeaconTaskDetails.Owner
    };

    var subNodes = new Mock<ISubNodeService>();
    var subnodes = new List<SubNodeModel>([new()
      {
        Id = Guid.NewGuid(),
        Owner = "user"
      }]);
    subNodes.Setup(x => x.List()).Returns(Task.FromResult(subnodes.AsEnumerable()));

    var logger = Mock.Of<ILogger<FilteringTermsService>>();

    var downstreamTasks = new Mock<IDownstreamTaskService>();
    downstreamTasks.Setup(x => x.Enqueue(It.IsAny<AvailabilityJob>(), It.IsAny<List<SubNodeModel>>()));

    var service = new FilteringTermsService(logger, DefaultOptions, subNodes.Object, downstreamTasks.Object);

    // Act
    await service.RequestUpdatedTerms();

    // Assert
    downstreamTasks.Verify(
      x => x.Enqueue(
        It.Is<CollectionAnalysisJob>(x =>
          x.Uuid.StartsWith(RelayBeaconTaskDetails.IdPrefix) &&
          x.Analysis == expectedTask.Analysis &&
          x.Code == expectedTask.Code &&
          x.Collection == expectedTask.Collection &&
          x.Owner == expectedTask.Owner),
        It.IsAny<List<SubNodeModel>>()),
      Times.Once);
  }

  [Fact]
  public void Map_SetsTermProperties()
  {
    var distributionRecord = new GenericDistributionRecord()
    {
      Code = "OMOP:12345",
      Collection = "",
      Count = 12345,
      Category = "Condition",
      OmopDescription = "My Fictional OMOP Term",
      OmopCode = 12345
    };

    var expected = new FilteringTerm()
    {
      Term = distributionRecord.Code,
      SourceCategory = distributionRecord.Category,
      Description = distributionRecord.OmopDescription,
    };

    var actual = FilteringTermsService.Map(distributionRecord);

    Assert.Equivalent(expected, actual);
  }

  [Theory]
  [InlineData(CodeCategory.Condition, null)]
  [InlineData(CodeCategory.Measurement, null)]
  [InlineData(CodeCategory.Observation, null)]
  [InlineData(CodeCategory.Ethnicity, "person")]
  [InlineData(CodeCategory.Race, "person")]
  [InlineData(CodeCategory.Gender, "person")]
  public void Map_SetsVarCat(string category, string? varcat)
  {
    var distributionRecord = new GenericDistributionRecord()
    {
      Code = "OMOP:12345",
      Collection = "",
      Count = 12345,
      Category = category,
      OmopDescription = "My Fictional OMOP Term",
      OmopCode = 12345
    };

    var actual = FilteringTermsService.Map(distributionRecord);

    Assert.Equal(varcat, actual.VarCat);
  }

  [Theory]
  [InlineData("Description", "OmopDescription", "OmopDescription")]
  [InlineData("", "OmopDescription", "OmopDescription")]
  [InlineData("Description", "", "Description")]
  [InlineData("", "", "")]
  public void Map_PrefersOmopDescription(string description, string omopDescription, string expected)
  {
    var distributionRecord = new GenericDistributionRecord()
    {
      Code = "OMOP:12345",
      Collection = "",
      Count = 12345,
      Category = "Measurement",
      Description = description,
      OmopDescription = omopDescription,
      OmopCode = 12345
    };

    var actual = FilteringTermsService.Map(distributionRecord);

    Assert.Equal(expected, actual.Description);
  }

  [Fact]
  public void Map_MapsList()
  {
    List<GenericDistributionRecord> distributionRecords = [
      new()
      {
        Code = "OMOP:12345",
        Collection = "",
        Count = 12345,
        Category = "Observation",
        OmopDescription = "An Observation",
        OmopCode = 12345
      },
      new()
      {
        Code = "OMOP:789",
        Collection = "",
        Count = 16,
        Category = "Gender",
        Description = "Female",
        OmopCode = 789
      },
      new()
      {
        Code = "OMOP:54321",
        Collection = "",
        Count = 100,
        Category = "Condition",
        Description = "A Condition",
        OmopDescription = "An OMOP Condition",
        OmopCode = 54321
      }
    ];

    List<FilteringTerm> expected = [
      new() {
        Term = "OMOP:12345",
        SourceCategory = "Observation",
        Description = "An Observation",
      },
      new() {
        Term = "OMOP:789",
        SourceCategory = "Gender",
        Description = "Female",
        VarCat = "person"
      },
      new() {
        Term = "OMOP:54321",
        SourceCategory = "Condition",
        Description = "An OMOP Condition",
      }
    ];

    var actual = FilteringTermsService.Map(distributionRecords);

    Assert.Collection(actual,
      x => Assert.Equivalent(expected[0], x),
      x => Assert.Equivalent(expected[1], x),
      x => Assert.Equivalent(expected[2], x));
  }
}
