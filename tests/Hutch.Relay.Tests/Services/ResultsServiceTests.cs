using Hutch.Rackit;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config;
using Hutch.Relay.Constants;
using Hutch.Relay.Models;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Hutch.Relay.Services.JobResultAggregators;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services;

public class ResultsServiceTests
{
  [Fact]
  public async Task PrepareFinalJobResult_SetsCorrectBaseProperties()
  {
    // Note that we don't test aggregation or obfuscation outputs here;
    // they are done in their respective service test suites

    var relayTask = new RelayTaskModel()
    {
      Id = Guid.NewGuid().ToString(),
      Collection = Guid.NewGuid().ToString(),
      Type = TaskTypes.TaskApi_Availability,
    };

    var expected = new JobResult()
    {
      Uuid = relayTask.Id,
      CollectionId = relayTask.Collection,
    };

    var tasks = new Mock<IRelayTaskService>();
    tasks.Setup(x =>
        x.ListSubTasks(
          It.Is<string>(y => y == relayTask.Id),
          It.Is<bool>(y => y == true)))
      .Returns(() => Task.FromResult<IEnumerable<RelaySubTaskModel>>(new List<RelaySubTaskModel>()));

    var aggregator = new Mock<IQueryResultAggregator>();
    aggregator
      .Setup(x => 
        x.Process(It.IsAny<List<RelaySubTaskModel>>()))
      .Returns(() => new() { Count = 0 });

    var resultsService = new ResultsService(
      null!,
      Options.Create<ApiClientOptions>(new()),
      null!,
      tasks.Object,
      aggregator.Object
    );

    var actual = await resultsService.PrepareFinalJobResult(relayTask);

    // Check the relevant base properties
    Assert.Equal(expected.Uuid, actual.Uuid);
    Assert.Equal(expected.CollectionId, actual.CollectionId);
    Assert.Equal(expected.Message, actual.Message);
    Assert.Equal(expected.ProtocolVersion, actual.ProtocolVersion);
    Assert.Equal(expected.Status, actual.Status);
  }
}
