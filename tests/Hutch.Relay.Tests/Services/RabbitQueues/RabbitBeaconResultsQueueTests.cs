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
  [Fact]
  public async Task Publish_NoMatchingQueue_Returns()
  {
    var channel = new Mock<IChannel>();
    channel.Setup(x => x.QueueDeclarePassiveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .Throws(new InvalidOperationException()); // TODO: Find out what ACTUALLY throws here

    var rabbit = new Mock<IRabbitConnectionManager>();
    rabbit.Setup(x => x.ConnectChannel(It.IsAny<string?>()))
      .Returns(() => Task.FromResult(channel.Object));


    var service = new RabbitBeaconResultsQueue(rabbit.Object);

    await service.Publish("test", 0);

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
    const string jobId = RelayBeaconTaskDetails.IdPrefix + queueName;
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
            JsonSerializer.Deserialize<int>(Encoding.UTF8.GetString(body.ToArray()), JsonSerializerOptions.Default) == count),
          It.IsAny<CancellationToken>()),
      Times.Once());
  }
}
