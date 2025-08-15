using System.Text;
using System.Text.Json;
using Hutch.Relay.Constants;
using RabbitMQ.Client;

namespace Hutch.Relay.Services.RabbitQueues;

public class RabbitBeaconResultsQueue(IRabbitConnectionManager rabbit) : IBeaconResultsQueue
{
  public async Task Publish(string jobId, int count)
  {
    var queueName = jobId.Replace(RelayBeaconTaskDetails.IdPrefix, string.Empty);
    
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
