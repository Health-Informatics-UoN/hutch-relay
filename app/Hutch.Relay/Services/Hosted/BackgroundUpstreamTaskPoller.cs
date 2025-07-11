using Hutch.Relay.Services.Contracts;

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
      // use a shortlived scope to run some db checks without keeping a context open
      using (var initScope = serviceScopeFactory.CreateScope())
      {
        var subnodes = initScope.ServiceProvider.GetRequiredService<ISubNodeService>();

        // Ensure we have subnodes before we start polling; this is considered critical
        subnodes.EnsureSubNodes();
      }

      // use a longer-lived scope to run the poller and its threads
      using var scope = serviceScopeFactory.CreateScope();
      var poller = scope.ServiceProvider.GetRequiredService<UpstreamTaskPoller>();

      await poller.PollAllQueues(stoppingToken);
    }
  }
}
