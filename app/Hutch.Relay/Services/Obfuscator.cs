using Hutch.Relay.Config;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Services;

public class Obfuscator(IOptions<ObfuscationOptions> obfuscationOptions)
{
  private readonly ObfuscationOptions _obfuscationOptions = obfuscationOptions.Value;

  /// <summary>
  /// Applies low number suppression to the value. If the value is greater than the threshold, it will remain the same. Otherwise, it will be set to 0.
  /// </summary>
  /// <param name="value">The value to apply suppression to.</param>
  /// <param name="threshold">The threshold at which (and below) values are suppressed.</param>
  /// <returns>The (potentially) suppressed value.</returns>
  public static int LowNumberSuppression(int value, int threshold)
    => value > threshold ? value : 0;

  /// <summary>
  /// Applies low number suppression to the value. If the value is greater than the threshold, it will remain the same. Otherwise, it will be set to 0.
  /// </summary>
  /// <param name="value">The value to apply suppression to.</param>
  /// <param name="options">Obfuscation options specifying the suppression threshold</param>
  /// <returns>The (potentially) suppressed value.</returns>
  public static int LowNumberSuppression(int value, ObfuscationOptions options)
    => LowNumberSuppression(value, options.LowNumberSuppressionThreshold);


  /// <summary>
  /// Applies rounding to the value. The value will be rounded to the nearest specified target number.
  /// </summary>
  /// <param name="value">The value to be rounded.</param>
  /// <param name="target">The target nearest factor to round to.</param>
  /// <returns>The rounded value.</returns>
  public static int Rounding(int value, int target)
    => target * (int)Math.Round((float)value / target);


  /// <summary>
  /// Applies rounding to the value. The value will be rounded to the nearest specified target number.
  /// </summary>
  /// <param name="value">The value to be rounded.</param>
  /// <param name="options">Obfuscation options specifying the rounding target</param>
  /// <returns>The rounded value.</returns>
  public static int Rounding(int value, ObfuscationOptions options)
    => Rounding(value, options.RoundingTarget);
  
  /// <summary>
  /// Applies obfuscation functions to the value based on the obfuscation options.
  /// </summary>
  /// <param name="value">The value to be obfuscated.</param>
  /// <param name="options">Obfuscation options to configure the obfuscation methods.</param>
  /// <returns>The obfuscated value.</returns>
  public static int Obfuscate(int value, ObfuscationOptions options)
  {
    if (options.LowNumberSuppressionThreshold > 0)
    {
      value = LowNumberSuppression(value, options);
    }

    if (options.RoundingTarget > 0)
    {
      value = Rounding(value, options);
    }

    return value;
  }

  /// <summary>
  /// Applies low number suppression to the value. If the value is greater than the threshold, it will remain the same. Otherwise, it will be set to 0.
  /// </summary>
  /// <param name="value">The value to apply suppression to.</param>
  /// <returns>The (potentially) suppressed value.</returns>
  public int LowNumberSuppression(int value)
    => LowNumberSuppression(value, _obfuscationOptions.LowNumberSuppressionThreshold);

  /// <summary>
  /// Applies rounding to the value. The value will be rounded to the nearest specified target number.
  /// </summary>
  /// <param name="value">The value to be rounded.</param>
  /// <returns>The rounded value.</returns>
  public int Rounding(int value)
    => Rounding(value, _obfuscationOptions.RoundingTarget);

  /// <summary>
  /// Applies obfuscation functions to the value based on the obfuscation options.
  /// </summary>
  /// <param name="value">The value to be obfuscated.</param>
  /// <returns>The obfuscated value.</returns>
  public int Obfuscate(int value)
    => Obfuscate(value, _obfuscationOptions);
}
