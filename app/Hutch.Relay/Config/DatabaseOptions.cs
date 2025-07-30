namespace Hutch.Relay.Config;

/// <summary>
/// <para>
/// Configuration for how Relay interacts with its local Database.
/// </para>
///
/// <para>
/// Note that the actual connection string is expected in the conventional <c>ConnectionStrings:Default</c> location.
/// </para>
/// </summary>
public class DatabaseOptions
{
  public static string Section { get; set; } = "Database";

  /// <summary>
  /// Whether Relay should automatically migrate to the latest (per the version of Relay being run) schema on startup if necessary.
  /// </summary>
  public bool ApplyMigrationsOnStartup { get; set; }

  /// <summary>
  /// Retain <see cref="Data.Entities.RelayTask"/> and <see cref="Data.Entities.RelaySubTask"/> records after they have been completed.
  /// </summary>
  public bool RetainCompletedTaskState { get; set; }
}
