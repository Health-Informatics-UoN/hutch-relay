using System.Threading.Channels;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Hutch.Relay.Services.RabbitQueues;

public class AsyncSingleMessageConsumer(IChannel channel) : AsyncDefaultBasicConsumer(channel)
{
  private readonly Channel<BasicDeliverEventArgs> _channel =
    System.Threading.Channels.Channel
      .CreateBounded<BasicDeliverEventArgs>(1);

  public async Task<BasicDeliverEventArgs> MessageDelivered(CancellationToken cancellationToken = new())
  {
    BasicDeliverEventArgs? result = null;

    // No need to loop as you might normally - we handle a single message only
    var ready = await _channel.Reader.WaitToReadAsync(cancellationToken);

    if (ready) _channel.Reader.TryRead(out result);

    return result ?? throw new InvalidOperationException();
  }

  public override async Task HandleBasicDeliverAsync(string consumerTag, ulong deliveryTag, bool redelivered,
    string exchange,
    string routingKey, IReadOnlyBasicProperties properties, ReadOnlyMemory<byte> body,
    CancellationToken cancellationToken = new())
  {
    // Write a message only if we haven't already completed the channel (which we do after one message)
    if (await _channel.Writer.WaitToWriteAsync(cancellationToken))
    {
      await _channel.Writer.WriteAsync(new(
          consumerTag,
          deliveryTag,
          redelivered,
          exchange,
          routingKey,
          properties,
          body,
          cancellationToken),
        cancellationToken);

      // Close the threading channel since this is a single message consumer only
      _channel.Writer.Complete();
    }
  }
}
