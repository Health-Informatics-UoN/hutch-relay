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

public static class ResultFileName
{
  public const string CodeDistribution = "code.distribution";

  public const string DemographicsDistribution = "demographics.distribution";
}

public static class ResultResponseStatus
{
  public const string Conflict = "CONFLICT";
}

// This is not exhaustive but represents known keys that we sometimes operate on
public static class Demographics
{
  public const string Age = "AGE";
  public const string Sex = "SEX";
  public const string Genomics = "GENOMICS";
}
