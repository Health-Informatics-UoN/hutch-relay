using Hutch.Relay.Constants;

namespace Hutch.Relay.Config;

public class ObfuscationOptions
{
  public int LowNumberSuppressionThreshold { get; set; } = ObfuscationDefaults.LowNumberSuppressionThreshold;
  public int RoundingTarget { get; set; } = ObfuscationDefaults.RoundingTarget;
}
