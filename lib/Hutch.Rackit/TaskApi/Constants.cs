namespace Hutch.Rackit.TaskApi;

public static class AnalysisType
{
  public const string Distribution = "DISTRIBUTION";
  public const string AnalyticsGenePhewas = "PHEWAS";

  // TODO: Confirm Cohort Analysis values from actual payloads
  // public const string AnalyticsGwas = "";
  // public const string AnalyticsGwasQuantitiveTrait = "";
  // public const string AnalyticsBurdenTest = "";
}

public static class DistributionCode
{
  public const string Generic = "GENERIC";

  public const string Demographics = "DEMOGRAPHICS";

  public const string Icd = "ICD-MAIN";
}
