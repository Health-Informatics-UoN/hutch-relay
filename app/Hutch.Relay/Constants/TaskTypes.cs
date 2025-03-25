// ReSharper disable InconsistentNaming
namespace Hutch.Relay.Constants;

public static class TaskTypes
{
  public const string TaskApi_Availability = "RQ.a";
  
  public const string TaskApi_CodeDistribution = "RQ.b.DISTRIBUTION.GENERIC";
  public const string TaskApi_DemographicsDistribution = "RQ.b.DISTRIBUTION.DEMOGRAPHICS";
  public const string TaskApi_IcdDistribution = "RQ.b.DISTRIBUTION.ICD-MAIN";
  public const string TaskApi_PhewasAnalysis = "RQ.b.PHEWAS.";
  
  // TODO: "c"
  // public const string TaskApi_GwasAnalysis = "RQ.c.GWAS";
}
