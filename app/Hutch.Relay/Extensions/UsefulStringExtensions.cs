namespace Hutch.Relay.Extensions;

public static class UsefulStringExtensions
{
  public static string ExtractAfterSubstring(this string input, string searchFor)
  {
    var index = input.IndexOf(searchFor, StringComparison.Ordinal);
    return
      index != -1
        ? input[(index + searchFor.Length)..]
        : string.Empty;
  }
}
