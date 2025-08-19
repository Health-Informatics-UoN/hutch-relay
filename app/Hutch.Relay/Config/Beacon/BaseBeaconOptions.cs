using Hutch.Relay.Config.Helpers;

namespace Hutch.Relay.Config.Beacon;

[ConfigSection(Features.Beacon)]
public class BaseBeaconOptions
{
  public MaturityAttributes MaturityAttributes { get; set; } = new();
  public SecurityAttributes SecurityAttributes { get; set; } = new();
}

public class MaturityAttributes
{
  public ProductionStatus ProductionStatus { get; set; }
}

public class SecurityAttributes
{
  public Granularity DefaultGranularity { get; set; }
  public List<ApiSecurityLevels> SecurityLevels { get; set; } = [];
}

public enum ProductionStatus
{
  DEV,
  TEST,
  PROD
}

public enum ApiSecurityLevels
{
  /// <summary>
  /// Any anonymous user can read the data
  /// </summary>
  PUBLIC,
  
  // Currently unsupported by Relay
  //REGISTERED	Only known users can read the data
  //CONTROLLED	Only specifically granted users can read the data
}

public enum Granularity
{
  /// <summary>
  /// returns 'true/false' responses
  /// </summary>
  boolean,
  
  /// <summary>
  /// adds the total number of positive results found
  /// </summary>
  count,
  
  // Unsupported by Relay
  // returns details for every document
  //Record
}
