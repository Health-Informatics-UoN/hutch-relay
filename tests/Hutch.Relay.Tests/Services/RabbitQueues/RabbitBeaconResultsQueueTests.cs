using System.Text;
using System.Text.Json;
using Hutch.Relay.Constants;
using Hutch.Relay.Services.RabbitQueues;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace Hutch.Relay.Tests.Services.RabbitQueues;

public class RabbitBeaconResultsQueueTests
{
  [Theory]
  [InlineData("test")]
  [InlineData(RelayBeaconTaskDetails.IdSuffix)]
  [InlineData("acbb45b2-3303-4296-96fb-7d4316cee722" + RelayBeaconTaskDetails.IdSuffix)]
  public async Task Publish_InvalidJobIdFormat_Throws(string jobId)
  {
    var rabbit = Mock.Of<IRabbitConnectionManager>();


    var service = new RabbitBeaconResultsQueue(rabbit);

    await Assert.ThrowsAsync<ArgumentException>(async () =>
      await service.Publish(jobId, 0));
  }

  [Fact]
  public async Task Publish_NoMatchingQueue_Returns()
  {
    const string queueName = "test";
    var jobId = Guid.NewGuid() + RelayBeaconTaskDetails.IdSuffix + queueName;

    var channel = new Mock<IChannel>();
    channel.Setup(x => x.QueueDeclarePassiveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .Throws(new InvalidOperationException()); // TODO: Find out what ACTUALLY throws here

    var rabbit = new Mock<IRabbitConnectionManager>();
    rabbit.Setup(x => x.ConnectChannel(It.IsAny<string?>()))
      .Returns(() => Task.FromResult(channel.Object));


    var service = new RabbitBeaconResultsQueue(rabbit.Object);

    await service.Publish(jobId, 0);

    channel.Verify(x =>
        x.BasicPublishAsync(
          It.IsAny<string>(),
          It.IsAny<string>(),
          It.IsAny<bool>(),
          It.IsAny<BasicProperties>(),
          It.IsAny<ReadOnlyMemory<byte>>(),
          It.IsAny<CancellationToken>()),
      Times.Never());
  }

  [Fact]
  public async Task Publish_MatchingQueue_Publishes()
  {
    const string queueName = "test";
    var jobId = Guid.NewGuid() + RelayBeaconTaskDetails.IdSuffix + queueName;
    const int count = 1234;

    var channel = new Mock<IChannel>();
    channel.Setup(x => x.QueueDeclarePassiveAsync(
      It.Is(queueName, StringComparer.InvariantCulture),
      It.IsAny<CancellationToken>()));

    var rabbit = new Mock<IRabbitConnectionManager>();
    rabbit.Setup(x => x.ConnectChannel(It.IsAny<string?>()))
      .Returns(() => Task.FromResult(channel.Object));

    var service = new RabbitBeaconResultsQueue(rabbit.Object);

    await service.Publish(jobId, count);

    channel.Verify(x =>
        x.BasicPublishAsync(
          It.Is(string.Empty, StringComparer.InvariantCulture),
          It.Is(queueName, StringComparer.InvariantCulture),
          It.IsAny<bool>(),
          It.IsAny<BasicProperties>(),
          It.Is<ReadOnlyMemory<byte>>(body =>
            JsonSerializer.Deserialize<int>(Encoding.UTF8.GetString(body.ToArray()), JsonSerializerOptions.Default) ==
            count),
          It.IsAny<CancellationToken>()),
      Times.Once());
  }

  [Fact]
  public async Task CreateResultsQueue_DeclaresQueueAndReturnsName()
  {
    const string expected = "test";

    var channel = new Mock<IChannel>();

    channel.Setup(x => x.QueueDeclareAsync(
        It.Is(string.Empty, StringComparer.InvariantCulture),
        It.Is<bool>(durable => !durable),
        It.Is<bool>(exclusive => exclusive),
        It.Is<bool>(autoDelete => autoDelete),
        It.Is<Dictionary<string, object?>>(args => args.ContainsKey("x-expires")),
        It.Is<bool>(passive => !passive),
        It.Is<bool>(noWait => !noWait),
        It.IsAny<CancellationToken>()))
      .Returns(() => Task.FromResult(new QueueDeclareOk(expected, 0, 0)))
      .Verifiable(Times.Once);

    var rabbit = new Mock<IRabbitConnectionManager>();
    rabbit.Setup(x => x.ConnectChannel(It.IsAny<string?>()))
      .Returns(() => Task.FromResult(channel.Object));

    var service = new RabbitBeaconResultsQueue(rabbit.Object);

    var actual = await service.CreateResultsQueue();

    channel.VerifyAll();

    Assert.Equal(expected, actual);
  }
}
