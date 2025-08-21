using System.Globalization;

namespace Hutch.Relay.Attributes;

[AttributeUsage(AttributeTargets.Assembly)]
internal class BuildTimestampAttribute(string value) : Attribute
{
  public DateTimeOffset BuildTimestamp { get; } = DateTimeOffset.ParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
}
