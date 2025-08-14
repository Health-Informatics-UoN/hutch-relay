using Hutch.Relay.Config;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Hutch.Relay.Services.RabbitQueues;

/// <summary>
/// This just helps with Lifecycle management of Dependency Injected Connections,
/// and provides some helpers for interacting wth RabbitMQ
/// </summary>
public class RabbitConnectionManager(
  IOptions<RelayTaskQueueOptions> options,
  ILogger<RabbitConnectionManager> logger) : IQueueConnectionManager, IAsyncDisposable
{
  private IConnection? _connection;

  private readonly ConnectionFactory _factory = new()
  {
    Uri = new(options.Value.ConnectionString)
  };
  
  public async Task<IChannel> ConnectChannel(string queueName)
  {
    if (_connection is not null && !_connection.IsOpen)
    {
      await _connection.DisposeAsync();
      _connection = null;
    }

    _connection ??= await _factory.CreateConnectionAsync();

    var channel = await _connection.CreateChannelAsync();

    await channel.QueueDeclareAsync( // TODO: what about exchanges?
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
      await using var channel = await ConnectChannel(queueName ?? "readyTest");
    }
    catch (Exception e) // It's OK that this is broad; any exception while trying to do this means the app is unusable.
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
