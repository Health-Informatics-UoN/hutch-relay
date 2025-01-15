using BasicTaskApi;
using Hutch.Rackit;
using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Configuration
var options = Options.Create(new ApiClientOptions
{
  // Fill in your connection details
});

// Test polling or just procedural checks?
var doPolling = args.Contains("--poll");
var pollFor = TimeSpan.FromMinutes(1); // If in a polling mode, how long do we run the app for?


#region Dependency Preparation

// Procedurally create our own dependencies in the right order
// (In an app with the .NET Hosting model these would be registered for Dependency Injection)

var httpClient = new HttpClient();
using var factory = LoggerFactory.Create(o =>
{
  o.AddConsole();
  o.SetMinimumLevel(LogLevel.Debug);
});

var logger = factory.CreateLogger<Program>();

var client = new TaskApiClient(httpClient, options, factory.CreateLogger<TaskApiClient>());

var taskHandler = new TaskHandler(factory.CreateLogger<TaskHandler>(), client);

#endregion

if (doPolling)
{
  #region Poll Availability

  // start polling availability jobs
  logger.LogInformation("Checking for Availability jobs, for the configured time");

  // set a timer to cancel polling after  the configured time
  var cts = new CancellationTokenSource();
  var timer = new System.Timers.Timer(pollFor)
  {
    AutoReset = false
  };
  timer.Elapsed += (s, e) =>
  {
    cts.Cancel();
    cts.Dispose();
  };
  timer.Start();

  // respond to polling
  var jobs = client.PollJobQueue<AvailabilityJob>()
    .WithCancellation(cts.Token);

  await foreach (var job in jobs)
  {
    await taskHandler.HandleAvailabilityJob(job);
  }

  #endregion
}
else
{
  #region Procedurally Check Each Queue

  // 1. Fetch an Availability job for the configured collection
  logger.LogInformation("Checking for Availability jobs...");

  var availabilityJob = await client.FetchNextJobAsync<AvailabilityJob>();

  // Handle the job if there is one, by returning a stock result
  if (availabilityJob is not null) await taskHandler.HandleAvailabilityJob(availabilityJob);

  else logger.LogInformation("No Availability jobs waiting!");

  // 2. Fetch a CollectionAnalysis job for the configured collection
  logger.LogInformation("Checking for Collection Analysis jobs...");

  var analysisJob = await client.FetchNextJobAsync<CollectionAnalysisJob>();

  // Handle the job if there is one, by returning a stock result
  if (analysisJob is not null) await taskHandler.HandleCollectionAnalysisJob(analysisJob);
  else logger.LogInformation("No Collection Analysis jobs waiting!");

  #endregion
}
