using System.Text;
using Hutch.Rackit.TaskApi.Models;

namespace Hutch.Rackit.Tests.ResultFileExtensionsTests;

public class WithDataTests
{
  [Fact]
  public void WhenString_SetsPropertiesCorrectly()
  {
    var resultFile = new ResultFile().WithData(Base64TestData.CodeDistributionRaw);
    
    var expectedLength = Encoding.UTF8.GetByteCount(Base64TestData.CodeDistributionRaw);
    
    Assert.Equal(Base64TestData.CodeDistributionBase64, resultFile.FileData);
    Assert.Equal(expectedLength, resultFile.FileSize);
  }
  
  [Fact]
  public void WhenBytes_SetsPropertiesCorrectly()
  {
    var bytes = Encoding.UTF8.GetBytes(Base64TestData.CodeDistributionRaw);
    
    var resultFile = new ResultFile().WithData(bytes);

    var expectedLength = bytes.Length;
    
    Assert.Equal(Base64TestData.CodeDistributionBase64, resultFile.FileData);
    Assert.Equal(expectedLength, resultFile.FileSize);
  }
}
