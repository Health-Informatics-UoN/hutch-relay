using Hutch.Relay.Helpers;
using Xunit;

namespace Hutch.Relay.Tests.HelperTests;

public class TaskTypeHelperTests
{
  [Theory]
  [InlineData("RQ.a", "a")]
  [InlineData("RQ.b.DISTRIBUTION.GENERIC", "b")]
  [InlineData("RQ.b.DISTRIBUTION.DEMOGRAPHICS", "b")]
  [InlineData("RQ.b.PHEWAS.", "b")]
  public void GetQueueType_ReturnsExpectedQueueType(
    string taskType,
    string expectedQueueType)
  {
    var result = TaskTypeHelper.GetQueueType(taskType);

    Assert.Equal(expectedQueueType, result);
  }
}
