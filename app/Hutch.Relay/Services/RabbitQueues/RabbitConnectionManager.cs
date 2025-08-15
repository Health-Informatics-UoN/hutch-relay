using Hutch.Relay.Config;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Hutch.Relay.Services.RabbitQueues;

/// <summary>
/// This just helps with Lifecycle management of Dependency Injected Connections,
/// and provides some helpers for interacting wth RabbitMQ
/// </summary>
public class RabbitConnectionManager(
  ILogger<RabbitConnectionManager> logger,
  IConnectionFactory factory) : IRabbitConnectionManager, IAsyncDisposable
{
  private IConnection? _connection;

  /// <summary>
  /// <para>Ensure the RabbitMQ Connection is ready, and return a new channel on the connection.</para>
  ///
  /// <para>If a queue name is provided, declare a classic queue with that name.</para>
  /// <para>For more complex queue declarations, recommend doing it manually to pass arguments and get the declaration result.</para>
  /// </summary>
  /// <param name="queueName"></param>
  /// <returns></returns>
  public async Task<IChannel> ConnectChannel(string? queueName = null)
  {
    if (_connection is not null && !_connection.IsOpen)
    {
      await _connection.DisposeAsync();
      _connection = null;
    }

    _connection ??= await factory.CreateConnectionAsync();

    var channel = await _connection.CreateChannelAsync();

    if (queueName is not null)
      await channel.QueueDeclareAsync(
        queue: queueName,
        durable: false,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    return channel;
  }

  public async Task<bool> IsReady(string? queueName = null)
  {
    try
    {
      await using var channel = await ConnectChannel(queueName);
    }
    catch (BrokerUnreachableException e)
    {
      logger.LogCritical(e, "{ExceptionMessage}", e.Message);
      return false;
    }

    return true;
  }

  public async ValueTask DisposeAsync()
  {
    if (_connection is not null)
    {
      await _connection.CloseAsync();
      await _connection.DisposeAsync();
    }
  }
}
