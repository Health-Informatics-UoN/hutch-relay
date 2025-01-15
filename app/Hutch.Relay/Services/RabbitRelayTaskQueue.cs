using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config;
using Hutch.Relay.Models;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Hutch.Relay.Services;

public class RabbitRelayTaskQueue(
  ILogger<RabbitRelayTaskQueue> logger,
  IOptions<RelayTaskQueueOptions> options)
  : IRelayTaskQueue, IDisposable, IAsyncDisposable
{
  private readonly ConnectionFactory _factory = new()
  {
    Uri = new(options.Value.ConnectionString)
  };

  private IConnection? _connection;

  private async Task<IChannel> ConnectChannel(string queueName)
  {
    if (_connection is not null && !_connection.IsOpen)
    {
      await _connection.DisposeAsync();
      _connection = null;
    }

    _connection ??= await _factory.CreateConnectionAsync();

    var channel = await _connection.CreateChannelAsync();

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
      await using var channel = await ConnectChannel(queueName ?? "readyTest");
    }
    catch (Exception e) // It's OK that this is broad; any exception while trying to do this means the app is unusable.
    {
      logger.LogCritical(e, "{ExceptionMessage}", e.Message);
      return false;
    }

    return true;
  }

  public async Task Send<T>(string subnodeId, T message) where T : TaskApiBaseResponse
  {
    await using var channel = await ConnectChannel(subnodeId);

    var body = Encoding.UTF8.GetBytes(
      JsonSerializer.Serialize(message));

    await channel.BasicPublishAsync(
      exchange: string.Empty,
      routingKey: subnodeId,
      body: body,
      mandatory: false,
      basicProperties: new BasicProperties
      {
        Type = typeof(T).Name
      });
  }

  public async Task<(Type, TaskApiBaseResponse)?> Pop(string subnodeId)
  {
    await using var channel = await ConnectChannel(subnodeId);

    var consumer = new AsyncEventingBasicConsumer(channel);
    consumer.ReceivedAsync += (model, ea) =>
    {
      var body = ea.Body.ToArray();

      return Task.CompletedTask;
    };

    // Get a message if there is one
    var message = await channel.BasicGetAsync(subnodeId, true);
    if (message is null) return null;

    // Resolve the type property to an actual CLR Type
    var type = message.BasicProperties.Type switch
    {
      nameof(AvailabilityJob) => typeof(AvailabilityJob),
      nameof(CollectionAnalysisJob) => typeof(CollectionAnalysisJob),
      _ => throw new InvalidOperationException(
        $"Unknown message type: {message.BasicProperties.Type ?? "null"}")
    };

    // Deserialize the body to the correct type
    TaskApiBaseResponse? task = type.Name switch
    {
      nameof(AvailabilityJob) => JsonSerializer.Deserialize<AvailabilityJob>(
        Encoding.UTF8.GetString(message.Body.ToArray())),
      nameof(CollectionAnalysisJob) => JsonSerializer.Deserialize<CollectionAnalysisJob>(
        Encoding.UTF8.GetString(message.Body.ToArray())),
      _ => throw new InvalidOperationException(
        $"Unknown message type: {message.BasicProperties.Type ?? "null"}")
    };

    if (task is null)
      throw new InvalidOperationException(
        $"Message body is not valid for specified task type: {message.BasicProperties.Type}");
    
    return (type, task);
  }

  public void Dispose()
  {
    _connection?.Dispose();
  }

  public async ValueTask DisposeAsync()
  {
    if (_connection is not null) await _connection.DisposeAsync();
  }
}
