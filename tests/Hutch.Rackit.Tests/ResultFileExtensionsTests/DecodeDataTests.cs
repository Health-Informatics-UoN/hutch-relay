using Hutch.Rackit.TaskApi.Models;

namespace Hutch.Rackit.Tests.ResultFileExtensionsTests;

// this is fairly light touch as we aren't intending to test .NET's base64 conversion,
// just that our method doesn't do anything weird
public class DecodeDataTests
{
  [Fact]
  public void InputDecodesToExpectedOutput()
  {
    const string expected = Base64TestData.CodeDistributionRaw;

    var resultFile = new ResultFile()
    {
      FileData = Base64TestData.CodeDistributionBase64
    };

    Assert.Equal(expected, resultFile.DecodeData());
  }
}
