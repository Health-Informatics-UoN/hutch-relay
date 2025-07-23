namespace Hutch.Relay.Startup.Cli.Core;

/// <summary>
/// An interface representing a Root Command that can be hosted within a .NET Application Host.
/// This interface allows the Root Command to access the CLI Host, which provides access to the Service
/// Provider and other host-related functionalities.
/// </summary>
public interface IHostedRootCommand
{
  public IHost? CliHost { get; set; }
}
