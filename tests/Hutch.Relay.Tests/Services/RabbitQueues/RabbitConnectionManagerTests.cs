using Hutch.Relay.Services.RabbitQueues;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace Hutch.Relay.Tests.Services.RabbitQueues;

public class RabbitConnectionManagerTests
{
  [Fact]
  public async Task IsReady_WhenConnectionFails_ReturnsFalse()
  {
    var factory = new Mock<IConnectionFactory>();
    factory.Setup(x =>
        x.CreateConnectionAsync(It.IsAny<CancellationToken>()))
      .Throws(new BrokerUnreachableException(new InvalidOperationException()));

    var rabbit = new RabbitConnectionManager(
      Mock.Of<ILogger<RabbitConnectionManager>>(),
      factory.Object);

    var actual = await rabbit.IsReady();

    Assert.False(actual);
  }

  [Fact]
  public async Task IsReady_SuccessfulConnection_ReturnsTrue()
  {
    var factory = new Mock<IConnectionFactory>();
    factory.Setup(x =>
        x.CreateConnectionAsync(It.IsAny<CancellationToken>()))
      .Returns(() => Task.FromResult(Mock.Of<IConnection>()));

    var rabbit = new RabbitConnectionManager(
      Mock.Of<ILogger<RabbitConnectionManager>>(),
      factory.Object);

    var actual = await rabbit.IsReady();

    Assert.True(actual);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("my-queue")]
  public async Task ConnectChannel_WhenQueueName_DeclaresQueue(string? queueName)
  {
    var channel = new Mock<IChannel>();

    var connection = new Mock<IConnection>();
    connection.Setup(x => x.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
      .Returns(() => Task.FromResult(channel.Object));

    var factory = new Mock<IConnectionFactory>();
    factory.Setup(x =>
        x.CreateConnectionAsync(It.IsAny<CancellationToken>()))
      .Returns(() => Task.FromResult(connection.Object));

    var rabbit = new RabbitConnectionManager(
      Mock.Of<ILogger<RabbitConnectionManager>>(),
      factory.Object);

    var actual = await rabbit.ConnectChannel(queueName);

    channel.Verify(
      x => x.QueueDeclareAsync(
        It.Is(queueName ?? string.Empty, StringComparer.InvariantCulture),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<IDictionary<string, object?>>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<CancellationToken>()),
      queueName is null ? Times.Never : Times.Once);
  }
}
