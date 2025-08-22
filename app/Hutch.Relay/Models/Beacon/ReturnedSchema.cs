namespace Hutch.Relay.Models.Beacon;

// Unclear if Relay will ever actually use this, since granularity dictates we never return actual entities in a response
public class ReturnedSchema
{
  public required string EntityType { get; set; }
  public required string Schema { get; set; }
}
