namespace Hutch.Relay.Config;


/// <summary>
/// Contains Feature Management IDs as constants.
/// </summary>
public static class Features
{
  // NOTE: Some Features may relate to Config Sections (via IFeatureOptionsModel)
  //   this is defined by using these constants as the OptionsModel section names
  public const string UpstreamTaskApi = "UpstreamTaskApi";
  public const string Beacon = "Beacon";
}
