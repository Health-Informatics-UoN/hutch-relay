using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Flurl;
using Hutch.Rackit.TaskApi.Contracts;
using Hutch.Rackit.TaskApi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hutch.Rackit.TaskApi;

/// <summary>
/// A client for interacting with the Task Api endpoints
/// </summary>
public class TaskApiClient(
  HttpClient client,
  IOptions<ApiClientOptions> configuredOptions,
  ILogger<TaskApiClient> logger
) : ITaskApiClient
{
  /// <summary>
  /// Default options for the service as configured
  /// </summary>
  public ApiClientOptions Options = configuredOptions.Value;

  /// <summary>
  /// Encode a username and password into the combined base64 format
  /// expected for a Basic Authentication header.
  /// </summary>
  /// <param name="username">The username to be included in the encoded result</param>
  /// <param name="password">The password to use in the encoded result</param>
  /// <returns>A base64 string of the input parameters</returns>
  internal static string EncodeCredentialsForBasicAuth(string username, string password)
    => Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));

  /// <summary>
  /// Repeatedly calls <see cref="FetchNextJobAsync"/> and returns jobs when found.
  /// </summary>
  /// <typeparam name="T">The type of job (and response model to be returned)</typeparam>
  /// <param name="options">The options specified to override the defaults</param>
  /// <param name="cancellationToken">A token used to cancel the polling loop</param>
  /// <returns>The next job of the requested type, when available.</returns>
  public async IAsyncEnumerable<T> PollJobQueue<T>(
    ApiClientOptions? options = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
    where T : TaskApiBaseResponse, new()
  {
    var pollingFrequency = options?.PollingFrequency ?? Options.PollingFrequency;

    while (true)
    {
      if (cancellationToken.IsCancellationRequested) break;

      var job = await FetchNextJobAsync<T>(options);

      if (job is not null) yield return job;

      // negative frequency runs only once, regardless whether job is found
      if (pollingFrequency < 0) break;

      try
      {
        if (pollingFrequency > 0) // 0 frequency runs without delay
          await Task.Delay(pollingFrequency, cancellationToken); // all other values cause a delay in ms
      }
      catch (TaskCanceledException)
      {
        break;
      }
    }
  }

  /// <summary>
  /// Calls <see cref="FetchNextJobAsync"/>, optionally with the options specified in the provided object.
  /// 
  /// Any missing options will fall back to the service's default configured options.
  /// </summary>
  /// <typeparam name="T">The type of job (and response model to be returned)</typeparam>
  /// <param name="options">The options specified to override the defaults</param>
  /// <returns>A model of the requested job type if one was found; <c>null</c> if not.</returns>
  /// <exception cref="ArgumentException">A required option is missing because it wasn't provided and is not present in the service defaults</exception>
  public async Task<T?> FetchNextJobAsync<T>(ApiClientOptions? options = null) where T : TaskApiBaseResponse, new()
  {
    static string exceptionMessage(string propertyName)
      => $"The property '{propertyName}' was not specified, and no default is available to fall back to.";

    return await FetchNextJobAsync<T>(
      options?.BaseUrl ?? Options.BaseUrl ?? throw new ArgumentException(exceptionMessage(nameof(options.BaseUrl))),
      options?.CollectionId ?? Options.CollectionId ??
      throw new ArgumentException(exceptionMessage(nameof(options.CollectionId))),
      options?.Username ?? Options.Username ?? throw new ArgumentException(exceptionMessage(nameof(options.Username))),
      options?.Password ?? Options.Password ?? throw new ArgumentException(exceptionMessage(nameof(options.Password)))
    );
  }

  /// <summary>
  /// Fetch the next query, if any, of the requested type.
  /// </summary>
  /// <typeparam name="T">The type of job (and response model to be returned)</typeparam>
  /// <param name="baseUrl">Base URL of the API instance to connect to.</param>
  /// <param name="collectionId">Collection ID to fetch query for.</param>
  /// <param name="username">Username to use when connecting to the API.</param>
  /// <param name="password">Password to use when connecting to the API.</param>
  /// <returns>A model of the requested query type if one was found; <c>null</c> if not.</returns>
  /// <exception cref="RackitApiClientException">An unknown type was requested, or an otherwise unexpected error occurred while interacting with the API.</exception>
  public async Task<T?> FetchNextJobAsync<T>(string baseUrl, string collectionId, string username, string password)
    where T : TaskApiBaseResponse, new()
  {
    var typeSuffix = new T() switch
    {
      AvailabilityJob => JobTypeSuffixes.Availability,
      CollectionAnalysisJob => JobTypeSuffixes.CollectionAnalysis,
      _ => throw new RackitApiClientException($"Unexpected Task API Response type requested: {typeof(T)}.")
    };

    var requestUrl = Url.Combine(
      baseUrl,
      TaskApiEndpoints.Base,
      TaskApiEndpoints.FetchNextJob,
      collectionId + typeSuffix);

    // TODO: reusable request helper?
    using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

    request.Headers.Authorization = new AuthenticationHeaderValue(
      "Basic",
      EncodeCredentialsForBasicAuth(username, password));

    var result = await client.SendAsync(request);

    if (result.IsSuccessStatusCode)
    {
      if (result.StatusCode == HttpStatusCode.NoContent)
      {
        logger.LogDebug("No Jobs of type {TypeName} waiting for {CollectionId}", typeof(T).Name, collectionId);
        return null;
      }

      try
      {
        return await result.Content.ReadFromJsonAsync<T>();
      }
      catch (JsonException e)
      {
        logger.LogError(e, "Invalid Response Format from Fetch Next Job Endpoint");

        var body = await result.Content.ReadAsStringAsync();
        logger.LogDebug("Invalid Response Body: {Body}", body);

        throw;
      }
    }
    else
    {
      var body = await result.Content.ReadAsStringAsync();
      logger.LogError("Fetch Next Job Endpoint Request failed: {StatusCode}", result.StatusCode);
      logger.LogDebug("Failure Response Body:\n{Body}", body);
      throw new RackitApiClientException($"Fetch Next Job Endpoint Request failed: {result.StatusCode}");
    }
  }

  private static StringContent AsHttpJsonString<T>(T value)
    => new(
      JsonSerializer.Serialize(value),
      Encoding.UTF8,
      "application/json");

  /// <summary>
  /// Post to the Results endpoint, and handle the response correctly.
  /// </summary>
  /// <param name="jobId">Job ID to submit results for.</param>
  /// <param name="result">The results to submit.</param>
  /// <param name="options">The options specified to override the defaults</param>
  /// <exception cref="ArgumentException">A required option is missing because it wasn't provided and is not present in the service defaults</exception>
  public async Task SubmitResultAsync(string jobId, JobResult result, ApiClientOptions? options = null)
  {
    await SubmitResultAsync(
      options?.BaseUrl ?? Options.BaseUrl ?? throw new ArgumentException(ExceptionMessage(nameof(options.BaseUrl))),
      options?.CollectionId ?? Options.CollectionId ??
      throw new ArgumentException(ExceptionMessage(nameof(options.CollectionId))),
      options?.Username ?? Options.Username ?? throw new ArgumentException(ExceptionMessage(nameof(options.Username))),
      options?.Password ?? Options.Password ?? throw new ArgumentException(ExceptionMessage(nameof(options.Password))),
      jobId,
      result
    );
    return;

    static string ExceptionMessage(string propertyName)
      => $"The property '{propertyName}' was not specified, and no default is available to fall back to.";
  }

  /// <summary>
  /// Post to the Results endpoint, and handle the response correctly.
  /// </summary>
  /// <param name="baseUrl">Base URL of the API instance to connect to.</param>
  /// <param name="collectionId">Collection ID to submit results for.</param>
  /// <param name="username">Username to use when connecting to the API.</param>
  /// <param name="password">Password to use when connecting to the API.</param>
  /// <param name="jobId">Job ID to submit results for.</param>
  /// <param name="result">The results to submit.</param>
  /// <exception cref="RackitApiClientException">An unsuccessful response was received from the remote Task API.</exception>
  public async Task SubmitResultAsync(string baseUrl, string collectionId, string username, string password,
    string jobId, JobResult result)
  {
    var requestUrl = Url.Combine(
      baseUrl,
      TaskApiEndpoints.Base,
      TaskApiEndpoints.SubmitResult,
      jobId,
      collectionId);

    using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

    request.Headers.Authorization = new AuthenticationHeaderValue(
      "Basic",
      EncodeCredentialsForBasicAuth(username, password));

    request.Content = AsHttpJsonString(result);

    var response = await client.SendAsync(request);

    var body = await response.Content.ReadAsStringAsync();

    if (body != "Job saved" || !response.IsSuccessStatusCode)
    {
      const string message = "Unsuccessful Response from Submit Results Endpoint";
      logger.LogError(message);
      logger.LogDebug("Response Body: {Body}", body);

      throw new RackitApiClientException(message, response);
    }
  }
}

/// <summary>
/// Lookup for request suffixes coded to job type
/// </summary>
internal static class JobTypeSuffixes
{
  /// <summary>
  /// Availability jobs
  /// </summary>
  public const string Availability = ".a";

  /// <summary>
  /// Analysis jobs against the Collection as a whole, such as AnalyticsGenePhewas, Code Distribution, Demographics Distribution, ICD Distribution
  /// </summary>
  public const string CollectionAnalysis = ".b";

  /// <summary>
  /// Analysis jobs against specified cohort selections, such as AnalyticsGwas, AnalyticsGwasQuantitiveTrait, AnalyticsBurdenTest
  /// </summary>
  public const string CohortAnalysis = ".c";
}

internal static class TaskApiEndpoints
{
  public const string Base = "link_connector_api/task";

  public const string QueueStatus = "queue";

  public const string FetchNextJob = "nextjob";

  public const string SubmitResult = "result";
}
