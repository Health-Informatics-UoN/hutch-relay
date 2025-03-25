using System.Text;
using Hutch.Rackit.TaskApi.Models;

namespace Hutch.Rackit.Tests.ResultFileExtensionsTests;

public class WithDataTests
{
  [Theory]
  [InlineData(Base64TestData.CodeDistributionRaw, Base64TestData.CodeDistributionBase64)]
  [InlineData(Base64TestData.DemographicsDistributionRaw, Base64TestData.DemographicsDistributionBase64)]
  public void WhenString_SetsPropertiesCorrectly(string raw, string expectedBase64)
  {
    var resultFile = new ResultFile().WithData(raw);

    var expectedLength = Encoding.UTF8.GetByteCount(raw);

    Assert.Equal(expectedBase64, resultFile.FileData);
    Assert.Equal(expectedLength, resultFile.FileSize);
  }

  [Theory]
  [InlineData(Base64TestData.CodeDistributionRaw, Base64TestData.CodeDistributionBase64)]
  [InlineData(Base64TestData.DemographicsDistributionRaw, Base64TestData.DemographicsDistributionBase64)]
  public void WhenBytes_SetsPropertiesCorrectly(string raw, string expectedBase64)
  {
    var bytes = Encoding.UTF8.GetBytes(raw);

    var resultFile = new ResultFile().WithData(bytes);

    var expectedLength = bytes.Length;

    Assert.Equal(expectedBase64, resultFile.FileData);
    Assert.Equal(expectedLength, resultFile.FileSize);
  }

  [Fact]
  public void WhenCodeDistributionRecords_SetsPropertiesCorrectly()
  {
    var resultFile = new ResultFile().WithData(Base64TestData.CodeDistributionList);

    var expectedLength = Encoding.UTF8.GetByteCount(Base64TestData.CodeDistributionRaw);

    Assert.Equal(Base64TestData.CodeDistributionBase64, resultFile.FileData);
    Assert.Equal(expectedLength, resultFile.FileSize);
  }

  [Fact]
  public void WhenDemographicsDistributionRecords_SetsPropertiesCorrectly()
  {
    var resultFile = new ResultFile().WithData(Base64TestData.DemographicsDistributionList);

    var expectedLength = Encoding.UTF8.GetByteCount(Base64TestData.DemographicsDistributionRaw);

    Assert.Equal(Base64TestData.DemographicsDistributionBase64, resultFile.FileData);
    Assert.Equal(expectedLength, resultFile.FileSize);
  }
}
