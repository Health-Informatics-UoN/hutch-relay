using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace Hutch.Rackit.TaskApi;

public static class CsvConfig
{
  public static CsvConfiguration Default => new CsvConfiguration(CultureInfo.CurrentCulture).ApplyDefault();

  public static CsvConfiguration GetDefault<T>()
  {
    var config = CsvConfiguration.FromAttributes<T>();
    return config.ApplyDefault();
  }

  private static CsvConfiguration ApplyDefault(this CsvConfiguration config)
  {
    config.MissingFieldFound = null; // The model will initialise missing fields
    config.Mode = CsvMode.NoEscape; // We are parsing TSV, not CSV (RFC 4180), so quotes in values are allowed

    return config;
  }
}
