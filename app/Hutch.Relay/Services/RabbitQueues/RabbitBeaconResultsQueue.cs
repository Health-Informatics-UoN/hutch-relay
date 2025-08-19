using System.Text;
using System.Text.Json;
using Hutch.Relay.Constants;
using Hutch.Relay.Extensions;
using Hutch.Relay.Services.Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Hutch.Relay.Services.RabbitQueues;

public class RabbitBeaconResultsQueue(IRabbitConnectionManager rabbit) : IBeaconResultsQueue
{
  public async Task<string> CreateResultsQueue()
  {
    await using var channel = await rabbit.ConnectChannel();

    var result = await channel.QueueDeclareAsync(
      string.Empty, // empty name forces server to generate a name

      // Auto Delete will delete the queue when the last consumer disconnects,
      // as long as there has been at least one consumer
      autoDelete: true,
      arguments: new Dictionary<string, object?>
      {
        // Queue Expiry guarantees queue deletion after specified time with no activity
        // Even if no consumers have ever connected
        ["x-expires"] = TimeSpan.FromMinutes(10).TotalMilliseconds // TODO: make configurable? 
      });

    return result.QueueName;
  }

  public async Task<int> AwaitResults(string queueName)
  {
    int? result = null;

    await using var channel = await rabbit.ConnectChannel();

    // check the job's transient queue is there
    await channel.QueueDeclarePassiveAsync(queueName);

    // Set up a consumer and event handlers
    var consumer = new AsyncEventingBasicConsumer(channel);
    consumer.ReceivedAsync += async (_, ea) =>
    {
      result = JsonSerializer.Deserialize<int>(
        Encoding.UTF8.GetString(
          ea.Body.ToArray()));

      // stop consuming as soon as we get a result
      await consumer.Channel.BasicCancelAsync(ea.ConsumerTag);
    };

    // "consume" the queue
    await channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);

    await channel.CloseAsync();

    return result ?? throw new InvalidOperationException(
      "Beacon Results Queue Channel closed before a result was retrieved!");
  }

  public async Task Publish(string jobId, int count)
  {
    var queueName = jobId.ExtractAfterSubstring(RelayBeaconTaskDetails.IdSuffix);
    if (string.IsNullOrWhiteSpace(queueName))
      throw new ArgumentException(
        "Failed to get Queue name. Beacon queries expecting results must include queue names in their ID.",
        jobId);

    await using var channel = await rabbit.ConnectChannel();

    // check the job's transient queue is there; we shouldn't publish to it if it isn't
    try
    {
      await channel.QueueDeclarePassiveAsync(queueName);
    }
    catch (Exception)
      // It's ok that this is broad;
      // Passive Declaration exceptions are not clearly documented
      // Any exception from the above try should cause us to not proceed
      // If it's a bigger queue problem it will show up elsewhere
    {
      return;
    }

    var body = Encoding.UTF8.GetBytes(
      JsonSerializer.Serialize(count));

    await channel.BasicPublishAsync(
      exchange: string.Empty,
      routingKey: queueName,
      body: body,
      mandatory: false,
      basicProperties: new BasicProperties
      {
        Type = nameof(Int32)
      });

    await channel.CloseAsync();
  }
}
