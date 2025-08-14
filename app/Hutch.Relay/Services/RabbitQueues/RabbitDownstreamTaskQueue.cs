using System.Text;
using System.Text.Json;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Config;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Hutch.Relay.Services.RabbitQueues;

public class RabbitDownstreamTaskQueue(RabbitConnectionManager rabbitConnect)
  : IDownstreamTaskQueue
{
  public async Task Publish<T>(string subnodeId, T message) where T : TaskApiBaseResponse
  {
    // TODO: Consider how we could optionally use a longer lived channel for publishing?
    // This could work due to how we only publish from the UpstreamTaskPoller thread and scope?
    await using var channel = await rabbitConnect.ConnectChannel(subnodeId);

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

    await channel.CloseAsync();
  }

  public async Task<(Type, TaskApiBaseResponse)?> Pop(string subnodeId)
  {
    // Due to the usage (on HTTP requests) sharing channels for consumption seems not worth it
    await using var channel = await rabbitConnect.ConnectChannel(subnodeId);

    // TODO: REMOVE WHEN WORKING
    // don't use the consumer model, since we aren't watching the queue continuously;
    // instead we check it on demand from HTTP requests
    // var consumer = new AsyncEventingBasicConsumer(channel);
    // consumer.ReceivedAsync += (model, ea) => // WTF?
    // {
    //   var body = ea.Body.ToArray();
    //
    //   return Task.CompletedTask;
    // };

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

    await channel.CloseAsync();

    return (type, task);
  }
}
