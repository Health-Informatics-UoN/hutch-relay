namespace Hutch.Relay.Config;

public class DownstreamUser
{
  public required string Password { get; set; }

  public Guid? SubNode { get; set; }
  public List<Guid>? SubNodes { get; set; }
}

public class DownstreamUsersOptions
{
  // key is username
  public Dictionary<string, DownstreamUser> DownstreamUsers { get; set; } = [];
}
