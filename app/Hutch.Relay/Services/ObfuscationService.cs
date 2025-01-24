using Hutch.Relay.Config;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Options;

namespace Hutch.Relay.Services;

public class ObfuscationService: IObfuscationService
{
  private readonly ObfuscationOptions _obfuscationOptions;

  public ObfuscationService(IOptions<ObfuscationOptions> obfuscationOptions)
  {
    _obfuscationOptions = obfuscationOptions.Value;
  }

  /// <summary>
  /// Applies low number suppression to the value. If the value is greater than the threshold, it will remain the same. Otherwise, it will be set to 0.
  /// </summary>
  /// <param name="value">The value to apply suppression to.</param>
  /// <returns>The suppressed value.</returns>
  public int LowNumberSuppression(int value)
  {
    return value > _obfuscationOptions.LowNumberSuppressionThreshold ? value : 0;
  }

  /// <summary>
  /// Applies rounding to the value. The value will be rounded to the nearest specified number.
  /// </summary>
  /// <param name="value">The value to be rounded.</param>
  /// <returns>The rounded value.</returns>
  public int Rounding(int value)
  {
    return _obfuscationOptions.RoundingTarget * (int)Math.Round((float)value / _obfuscationOptions.RoundingTarget);
  }

  /// <summary>
  /// Applies obfuscation functions to the value based on the obfuscation options.
  /// </summary>
  /// <param name="value">The value to be obfuscated.</param>
  /// <returns>The obfuscated value.</returns>
  public int Obfuscate(int value)
  {
    if (_obfuscationOptions.LowNumberSuppressionThreshold > 0)
    {
      value = LowNumberSuppression(value);
    }

    if (_obfuscationOptions.RoundingTarget > 0)
    {
      value = Rounding(value);
    }

    return value;
  }
}
