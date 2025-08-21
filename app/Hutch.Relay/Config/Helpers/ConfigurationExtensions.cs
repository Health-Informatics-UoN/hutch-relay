namespace Hutch.Relay.Config.Helpers;

public static class ConfigurationExtensions
{
  /// <summary>
  /// Configure an OptionsModel by implicitly deriving the Config Section from the type `T`
  /// </summary>
  /// <typeparam name="T">The class to derive the Config Section from. Uses the classname without the `Options` suffix, or <see cref="ConfigSectionAttribute"/></typeparam>
  /// <param name="s"></param>
  /// <returns></returns>
  public static IServiceCollection Configure<T>(this IServiceCollection s)
    where T : class
  {
    var config = s.BuildServiceProvider().GetRequiredService<IConfiguration>();
    s.Configure<T>(config.GetSection<T>());

    return s;
  }

  /// <summary>
  /// Get a Config Section by implicitly deriving the Config Section name from the type `T`
  /// </summary>
  /// <typeparam name="T">The class to derive the Config Section from. Uses the classname without the `Options` suffix, or <see cref="ConfigSectionAttribute"/></typeparam>
  /// <param name="config"></param>
  /// <returns></returns>
  public static IConfiguration GetSection<T>(this IConfiguration config)
    where T : class
    => config.GetSection(OptionsModel<T>.Section);

  public static IConfiguration GetSection(this IConfiguration config, Type optionsModelType)
    => config.GetSection(OptionsModel.GetSection(optionsModelType));

  /// <summary>
  /// Get a Config Section by implicitly deriving the Config Section name from the type `T`
  /// </summary>
  /// <typeparam name="T">The class to derive the Config Section from. Uses the classname without the `Options` suffix, or <see cref="ConfigSectionAttribute"/></typeparam>
  /// <param name="config"></param>
  /// <returns></returns>
  public static IConfiguration GetRequiredSection<T>(this IConfiguration config)
    where T : class
    => config.GetRequiredSection(OptionsModel<T>.Section);

  public static IConfiguration GetRequiredSection(this IConfiguration config, Type optionsModelType)
    => config.GetRequiredSection(OptionsModel.GetSection(optionsModelType));

  /// <summary>
  /// <para>For any passed <see cref="IFeatureOptionsModel" /> types, declares them as Feature Flags (per Microsoft Feature Management).</para>
  /// <para>The Feature's enabled state will be set based on binding the IFeatureOptionsModel to its Config Section</para>
  /// </summary>
  /// <param name="config"></param>
  /// <param name="sections">A list of Configuration Section names to declare as Feature Flags</param>
  /// <returns></returns>
  public static ConfigurationManager DeclareOptionsModelFeatures(this ConfigurationManager config, List<Type> featureOptionsModelTypes)
  {
    var features = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    // TODO: Support working with actual pre-declared features too, probably by offsetting `i`
    foreach (var (featureType, i) in featureOptionsModelTypes.Select((v, i) => (v, i)))
    {
      if (Activator.CreateInstance(featureType) is not IFeatureOptionsModel feature) continue;

      var section = OptionsModel.GetSection(featureType);
      config.GetSection(section).Bind(feature);

      features[$"feature_management:feature_flags:{i}:id"] = section;
      features[$"feature_management:feature_flags:{i}:enabled"] = feature.Enable
        ? bool.TrueString
        : bool.FalseString;
    }

    config.AddInMemoryCollection(features);

    return config;
  }

  /// <summary>
  /// Check if a <see cref="IFeatureOptionsModel"/> is Enabled. The Config Section name is implicitly derived from the type `T`.
  /// </summary>
  /// <typeparam name="T">The class to derive the Config Section and bind an Options model from. Uses the classname without the `Options` suffix, or <see cref="ConfigSectionAttribute"/></typeparam>
  /// <param name="config"></param>
  /// <returns></returns>
  public static bool IsEnabled<T>(this ConfigurationManager config)
    where T : class, IFeatureOptionsModel
    => config.GetSection<T>().Get<T>()?.Enable ?? false;

  /// <summary>
  /// Check if a <see cref="IFeatureOptionsModel"/> is Enabled. The Config Section name is implicitly derived from the type `T`.
  /// Non IFeatureOptionsModel types will return `false`.
  /// </summary>
  /// <param name="type">The class to derive the Config Section and bind an Options model from. Uses the classname without the `Options` suffix, or <see cref="ConfigSectionAttribute"/></typeparam>
  /// <param name="config"></param>
  /// <returns></returns>
  public static bool IsEnabled(this ConfigurationManager config, Type type)
  {
    if (Activator.CreateInstance(type) is not IFeatureOptionsModel feature) return false;

    var section = OptionsModel.GetSection(type);
    config.GetSection(section).Bind(feature);

    return feature.Enable;
  }
}
