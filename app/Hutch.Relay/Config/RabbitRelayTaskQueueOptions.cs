namespace Hutch.Relay.Config;

public class RelayTaskQueueOptions
{
  public static string Section { get; set; } = "RelayTaskQueue";

  public string ConnectionString { get; set; } = "amqp://localhost";
}
