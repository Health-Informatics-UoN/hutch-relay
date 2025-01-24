using Hutch.Rackit;
using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Services.Hosted;

/// <summary>
/// This is a hosted service for monitoring RelayTasks
/// </summary>
public class TaskCompletionHostedService(
  IServiceScopeFactory serviceScopeFactory,
  ILogger<TaskCompletionHostedService> logger) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      using var scope = serviceScopeFactory.CreateScope();
      var taskResultsService = scope.ServiceProvider.GetRequiredService<ResultsService>();
      
      logger.LogInformation("Finding Tasks that are expiring...");
      
      await taskResultsService.HandleResultsToExpire();
      await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
    }
  }
}
