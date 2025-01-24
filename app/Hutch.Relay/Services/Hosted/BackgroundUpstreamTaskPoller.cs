namespace Hutch.Relay.Services.Hosted;

/// <summary>
/// This is a background worker (IHostedService) for polling for tasks from an upstream system (e.g. Relay or BC|RQuest)
/// </summary>
public class BackgroundUpstreamTaskPoller(
  IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      using var scope = serviceScopeFactory.CreateScope();
      var poller = scope.ServiceProvider.GetRequiredService<UpstreamTaskPoller>();

      await poller.PollAllQueues(stoppingToken);
    }
  }
}
