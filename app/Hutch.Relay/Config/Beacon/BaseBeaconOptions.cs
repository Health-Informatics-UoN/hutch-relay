namespace Hutch.Relay.Config.Beacon;

public class BaseBeaconOptions
{
  public MaturityAttributes MaturityAttributes { get; set; } = new();
  public SecurityAttributes SecurityAttributes { get; set; } = new();
}

public class MaturityAttributes
{
  public string ProductionStatus { get; set; } = "DEV"; // TODO: enums in dotnet config?
}

public class SecurityAttributes
{
  public string DefaultGranularity { get; set; } = "boolean";
  public string SecurityLevels { get; set; } = "PUBLIC";
}
