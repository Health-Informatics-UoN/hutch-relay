using Hutch.Relay.Constants;

namespace Hutch.Relay.Config;

public class ObfuscationOptions
{
  public static string Section { get; set; } = "Obfuscation";

  public int LowNumberSuppressionThreshold { get; set; } = ObfuscationDefaults.LowNumberSuppressionThreshold;
  public int RoundingTarget { get; set; } = ObfuscationDefaults.RoundingTarget;
}
