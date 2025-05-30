using Hutch.Rackit;

namespace Hutch.Relay.Config;

public class TaskApiPollingOptions : ApiClientOptions
{
  /// <summary>
  /// <para>
  /// Whether to resume polling after a non-critical error occurs, swallowing (but logging) the exception.
  /// May be desirable in some environments.
  /// </para>
  /// <para>
  /// Container orchestrated (k8s, compose) or systemd service environments should prefer to always terminate,
  /// and control restarting the application themselves.
  /// </para>
  /// </summary>
  public bool ResumeOnError { get; set; } = false;

  /// <summary>
  /// The delay in seconds to wait before resuming polling after an error occurs.
  /// </summary>
  public int ErrorDelay { get; set; } = 5;
}