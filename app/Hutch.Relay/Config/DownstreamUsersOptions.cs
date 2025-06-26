namespace Hutch.Relay.Config;

public class DownstreamUser
{
  public required string Password { get; set; }

  public Guid? SubNode { get; set; }
  public List<Guid>? SubNodes { get; set; }
}

// keys are usernames
public class DownstreamUsersOptions : Dictionary<string, DownstreamUser> { }
