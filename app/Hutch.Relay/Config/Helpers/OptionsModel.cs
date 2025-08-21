using System.Reflection;

namespace Hutch.Relay.Config.Helpers;

/// <summary>
/// An Attribute for specifying a custom (override) Config Section name
/// which can be resolved by <see cref="IOptionsModel{T}"/>.
/// </summary>
/// <param name="section"></param>
[AttributeUsage(AttributeTargets.Class)]
public class ConfigSectionAttribute(string section) : Attribute
{
  public string Section { get; } = section;
}

/// <summary>
/// <para>Helper to resolve Config Section name for an Options Model style class.</para>
/// <para>Defaults to the classname with the `Options` suffix (if any) removed; can be overridden with <see cref="ConfigSectionAttribute"/>.</para>
/// </summary>
/// <typeparam name="T">The Class to resolve a Config Section name for.</typeparam>
public static class OptionsModel<T>
  where T : class
{
  /// <summary>
  /// Config Section name for `T`
  /// </summary>
  public static string Section
  {
    get
    {
      var derivedType = typeof(T);

      return OptionsModel.GetSection(derivedType);
    }
  }
}

public static class OptionsModel
{
  public static string GetSection(Type type)
  {
    var typeInfo = type.GetTypeInfo();

    // ConfigSectionAttribute overrides classname convention
    var attributes = typeInfo.GetCustomAttributes();
    foreach (var attribute in attributes)
      if (attribute is ConfigSectionAttribute sectionAttribute)
        return sectionAttribute.Section;

    // Remove suffix from classname and use that
    return type.Name.Replace("Options", "");
  }
}
