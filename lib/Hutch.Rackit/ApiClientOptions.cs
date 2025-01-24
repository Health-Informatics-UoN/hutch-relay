using System.Text;

namespace Hutch.Rackit;

/// <summary>
/// Options model for the API Client Services
/// </summary>
public class ApiClientOptions
{
  /// <summary>
  /// Base Url of the API to interact with
  /// </summary>
  public string? BaseUrl { get; set; }

  /// <summary>
  /// The Collection ID to use with API interactions
  /// </summary>
  public string? CollectionId { get; set; }

  /// <summary>
  /// Username to access the API. Passed in Basic Auth header.
  /// </summary>
  public string? Username { get; set; }

  /// <summary>
  /// Password to access the API. Passed in Basic Auth header.
  /// </summary>
  public string? Password { get; set; }

  /// <summary>
  /// Time in milliseconds that endpoint polling methods will wait between requests
  /// </summary>
  public int PollingFrequency { get; set; } = 5000;
}
