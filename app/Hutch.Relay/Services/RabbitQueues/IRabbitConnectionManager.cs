using Hutch.Relay.Services.Contracts;
using RabbitMQ.Client;

namespace Hutch.Relay.Services.RabbitQueues;

public interface IRabbitConnectionManager : IQueueConnectionManager
{
  /// <summary>
  /// <para>Ensure the RabbitMQ Connection is ready, and return a new channel on the connection.</para>
  ///
  /// <para>If a queue name is provided, declare a classic queue with that name.</para>
  /// <para>For more complex queue declarations, recommend doing it manually to pass arguments and get the declaration result.</para>
  /// </summary>
  /// <param name="queueName"></param>
  /// <returns></returns>
  Task<IChannel> ConnectChannel(string? queueName = null);
}
