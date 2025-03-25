using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Models;
using Hutch.Relay.Services.JobResultAggregators;
using Xunit;

namespace Hutch.Relay.Tests.Services.QueryResultAggregators;

public class AvailabilityAggregatorTests
{
  // We don't do in depth obfuscation tests here; they are done in the Obfuscator test suite.
  // But we do attempt to ensure that the Obfuscator is being applied to aggregate outputs
  
  [Fact]
  public void WhenNoSubTasks_WhenNoObfuscation_ReturnEmptyQueryResult()
  {
    var subTasks = new List<RelaySubTaskModel>();
    
    var expected = new QueryResult
    {
      Count = 0,
      Files = [],
      DatasetCount = 0
    };

    var aggregator = new AvailabilityAggregator();

    var actual = aggregator.Process(subTasks, new());

    Assert.Equivalent(expected, actual);
    
    // TODO: spy to check Obfuscator calls? I guess we can't do that with the static methods...
  }
}
