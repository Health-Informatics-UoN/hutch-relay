using Hutch.Relay.Config;
using Hutch.Relay.Constants;

namespace Hutch.Relay.Services;

/// <summary>
///   <list type="bullet">
///     <listheader>An Obfuscator interface that implements Relay's obfuscation routines:</listheader>
///     <item>The individual obfuscation methods as static math functions</item>
///     <item>
///       A single public <see cref="Obfuscate(int,Hutch.Relay.Config.ObfuscationOptions)"/> entrypoint for consumers to use.
///       Implementing this on the interface ensures it does not differ between implementations
///     </item>
///     <item>
///       An overridable method for supplying the <see cref="ObfuscationOptions"/> to configure the Obfuscation behaviour.
///       By this implementations can choose how they come by their options.
///     </item>
///   </list>
/// </summary>
public interface IObfuscator
{
  /// <summary>
  /// Applies low number suppression to the value. If the value is greater than or equal the threshold, it will remain the same. Otherwise, it will be set to 0.
  /// </summary>
  /// <param name="value">The value to apply suppression to.</param>
  /// <param name="threshold">The threshold below which values are suppressed.</param>
  /// <returns>The (potentially) suppressed value.</returns>
  protected static int LowNumberSuppression(int value, int threshold = ObfuscationDefaults.LowNumberSuppressionThreshold)
    => value >= threshold ? value : 0;

  /// <summary>
  /// Applies rounding to the value. The value will be rounded to the nearest specified target number.
  /// </summary>
  /// <param name="value">The value to be rounded.</param>
  /// <param name="target">The target nearest factor to round to.</param>
  /// <returns>The rounded value.</returns>
  protected static int Rounding(int value, int target = ObfuscationDefaults.RoundingTarget)
    => target * (int)Math.Round((float)value / target);

  /// <summary>
  /// Applies obfuscation functions to the value based on the obfuscation options.
  /// </summary>
  /// <param name="value">The value to be obfuscated.</param>
  /// <param name="options">Obfuscation options to configure the obfuscation methods.</param>
  /// <returns>The obfuscated value.</returns>
  protected static int Obfuscate(int value, ObfuscationOptions options)
  {
    if (options.LowNumberSuppressionThreshold > 0)
    {
      value = LowNumberSuppression(value, options.LowNumberSuppressionThreshold);
    }

    if (options.RoundingTarget > 0)
    {
      value = Rounding(value, options.RoundingTarget);
    }

    return value;
  }

  /// <summary>
  /// Must be defined by implementors to provide a mechanism of supplying <see cref="ObfuscationOptions"/>
  /// to the <see cref="Obfuscate(int)"/> method
  /// </summary>
  /// <returns>An instance of <see cref="ObfuscationOptions"/></returns>
  protected ObfuscationOptions GetObfuscationOptions();

  /// <summary>
  /// Applies obfuscation functions to the value.
  /// </summary>
  /// <param name="value">The value to be obfuscated.</param>
  /// <returns>The obfuscated value.</returns>
  public int Obfuscate(int value)
    => Obfuscate(value, GetObfuscationOptions());
}
