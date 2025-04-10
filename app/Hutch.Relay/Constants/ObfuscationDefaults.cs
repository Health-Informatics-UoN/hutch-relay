namespace Hutch.Relay.Constants;

/// <summary>
/// It's useful to set default non-zero values for Obfuscation in a number of places for safety,
/// e.g. not just in configuration but also in the actual methods
/// These constants allow that without scattering magic numbers across the codebase <3
/// </summary>
public class ObfuscationDefaults
{
  public const int LowNumberSuppressionThreshold = 10;
  public const int RoundingTarget = 10;
}
