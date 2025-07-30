/// <summary>
/// Helpers for making configuring Microsoft's FeatureManagement simpler for basic use cases
/// </summary>
public static class ConfigurationManagerExtensions
{
  /// <summary>
  /// <para>For any passed Config Section names, declares them as Feature Flags (per Microsoft Feature Management).</para>
  /// <para>Any sections that contain a boolean `Enable` key will also set the Feature's enabled state, otehrwise enabled will be false</para>
  /// </summary>
  /// <param name="config"></param>
  /// <param name="sections">A list of Configuration Section names to declare as Feature Flags</param>
  /// <returns></returns>
  public static ConfigurationManager DeclareSectionFeatures(this ConfigurationManager config, List<string> sections)
  {
    var features = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    foreach (var (section, i) in sections.Select((v, i) => (v, i)))
    {
      features[$"feature_management:feature_flags:{i}:id"] = section;
      features[$"feature_management:feature_flags:{i}:enabled"] = config.IsSectionEnabled(section)
        ? bool.TrueString
        : bool.FalseString;
    }

    return config;
  }

  /// <summary>
  /// Check if a config section has an `Enable` key set to `true`.
  /// </summary>
  /// <param name="config"></param>
  /// <param name="section"></param>
  /// <returns></returns>
  public static bool IsSectionEnabled(this ConfigurationManager config, string section)
    => config.GetSection(section).GetValue<bool>("Enable");
}
