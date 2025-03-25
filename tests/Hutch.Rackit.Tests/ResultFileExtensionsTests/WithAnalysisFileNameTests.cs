using Hutch.Rackit.TaskApi;
using Hutch.Rackit.TaskApi.Models;

namespace Hutch.Rackit.Tests.ResultFileExtensionsTests;

public class WithAnalysisFileNameTests
{
  private const string _garbageString = "garbage";
  
  [Theory]
  [InlineData("", "")]
  [InlineData(_garbageString, "")]
  [InlineData(AnalysisType.AnalyticsGenePhewas, "")]
  [InlineData(AnalysisType.AnalyticsGenePhewas, _garbageString)]
  [InlineData(AnalysisType.Distribution, "")]
  [InlineData(AnalysisType.Distribution, _garbageString)]
  [InlineData(AnalysisType.Distribution, DistributionCode.Icd)]
  [InlineData(AnalysisType.AnalyticsGenePhewas, DistributionCode.Generic)]
  public void WhenUnsupportedAnalysis_ShouldThrowNotImplemented(
    string analysisType, string analysisCode)
  {
    var resultFile = new ResultFile();
    
    Assert.Throws<NotImplementedException>(() => 
      resultFile.WithAnalysisFileName(analysisCode, analysisCode));
  }
}
