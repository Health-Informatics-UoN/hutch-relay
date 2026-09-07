namespace Hutch.Relay.Helpers;

public static class TaskTypeHelper
{
  public static string GetQueueType(string taskType)
  {
    if (!taskType.StartsWith("RQ."))
      return taskType;

    var type = taskType[3..];

    var separatorIndex = type.IndexOf('.');

    return separatorIndex >= 0
      ? type[..separatorIndex]
      : type;
  }
}
