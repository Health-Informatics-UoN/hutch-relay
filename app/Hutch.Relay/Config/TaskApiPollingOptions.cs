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

  /// <summary>
  /// <para>Specify which Job Type queues Relay should poll against in the Upstream Task API.</para>
  /// <para>Can be an empty string, a `*`, or a list of comma separated Task API Job Type codes.</para>
  /// <para>An empty string will cause Relay to poll single "unified" queue for jobs of any type, e.g. when connecting to Upstream Relays</para>
  /// <para>A comma separated list will cause Relay to poll separate queues for each supported type listed, in parallel, e.g. when connecting to upstream RQuest</para>
  /// <para>Defaults to <c>*</c> polling all supported types from separate queues (standard RQuest behaviour)</para>
  /// </summary>
  public string QueueTypes { get; set; } = "*";
}
