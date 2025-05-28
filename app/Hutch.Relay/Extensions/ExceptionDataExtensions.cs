namespace Hutch.Relay.Extensions;

public static class ExceptionDataExtensions
{
  public static LogLevel? LogLevel(this Exception e)
  {
    return e.Data.Contains(nameof(LogLevel)) && e.Data[nameof(LogLevel)] is LogLevel logLevel ? logLevel : null;
  }

  public static T WithLogLevel<T>(this T e, LogLevel logLevel) where T : Exception
  {
    e.Data.Add(nameof(LogLevel), Microsoft.Extensions.Logging.LogLevel.Critical);
    return e;
  }
}
