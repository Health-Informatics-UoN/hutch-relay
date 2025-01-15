namespace Hutch.Relay.Services.Contracts;

public interface IObfuscationService
{

  /// <summary>
  /// Suppresses numbers that are below a specified threshold.
  /// </summary>
  /// <param name="value">The value to be checked and suppressed.</param>
  /// <returns>The original value if it is above the threshold, otherwise 0.</returns>
  int LowNumberSuppression(int value);

  /// <summary>
  /// Rounds a value to the nearest specified number.
  /// </summary>
  /// <param name="value">The value to be rounded.</param>
  /// <returns>The value rounded to the nearest specified number.</returns>
  int Rounding(int value);


  /// <summary>
  /// Applies obfuscation functions to the value based on the obfuscation options.
  /// </summary>
  /// <param name="value">The value to be obfuscated.</param>
  /// <returns>The obfuscated value.</returns>
  int Obfuscate(int value);
}
