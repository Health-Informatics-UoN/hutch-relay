using Hutch.Rackit.TaskApi.Models;

namespace Hutch.Rackit.TaskApi.Contracts;

public interface ITaskApiClient
{
  /// <summary>
  /// Repeatedly calls <see cref="FetchNextJobAsync"/> and returns jobs when found.
  /// </summary>
  /// <typeparam name="T">The type of job (and response model to be returned)</typeparam>
  /// <param name="options">The options specified to override the defaults</param>
  /// <param name="cancellationToken">A token used to cancel the polling loop</param>
  /// <returns>The next job of the requested type, when available.</returns>
  public IAsyncEnumerable<T> PollJobQueue<T>(
    ApiClientOptions? options = null,
    CancellationToken cancellationToken = default)
    where T : TaskApiBaseResponse, new();

  /// <summary>
  /// Calls <see cref="FetchNextJobAsync"/>, optionally with the options specified in the provided object.
  /// 
  /// Any missing options will fall back to the service's default configured options.
  /// </summary>
  /// <typeparam name="T">The type of job (and response model to be returned)</typeparam>
  /// <param name="options">The options specified to override the defaults</param>
  /// <returns>A model of the requested job type if one was found; <c>null</c> if not.</returns>
  /// <exception cref="ArgumentException">A required option is missing because it wasn't provided and is not present in the service defaults</exception>
  public Task<T?> FetchNextJobAsync<T>(ApiClientOptions? options = null) where T : TaskApiBaseResponse, new();

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
  public Task<T?> FetchNextJobAsync<T>(string baseUrl, string collectionId, string username, string password)
    where T : TaskApiBaseResponse, new();

  /// <summary>
  /// Post to the Results endpoint, and handle the response correctly.
  /// </summary>
  /// <param name="jobId">Job ID to submit results for.</param>
  /// <param name="result">The results to submit.</param>
  /// <param name="options">The options specified to override the defaults</param>
  /// <exception cref="ArgumentException">A required option is missing because it wasn't provided and is not present in the service defaults</exception>
  public Task SubmitResultAsync(string jobId, JobResult result, ApiClientOptions? options = null);

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
  public Task SubmitResultAsync(string baseUrl, string collectionId, string username, string password,
    string jobId, JobResult result);
}
