using Hutch.Relay.Config;
using Hutch.Relay.Constants;
using Hutch.Relay.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hutch.Relay.Tests.Services;

public class ObfuscatorTests
{
// create obfuscation object
  [Theory]
  // test both rounding and suppression (something to be suppressed, rounding won't matter)
  [InlineData(50, 10, 27, 0)]

  // test both rounding and suppression (something to be rounded, not suppressed).
  [InlineData(10, 10, 2718, 2720)]

  //test suppression without rounding. 
  [InlineData(20, 0, 2718, 2718)]
  // test negative number rounding target
  [InlineData(20, -1, 2718, 2718)]
  // test threshold equality
  [InlineData(2718, 0, 2718, 2718)]
  [InlineData(2719, 0, 2718, 0)]

  // test obfuscation on something to be rounded up/down
  [InlineData(0, 10, 2718, 2720)]
  [InlineData(0, 10, 271, 270)]
  //test negative number threshold target
  [InlineData(-1, 10, 271, 270)]
  // test neither rounding nor lns
  [InlineData(0, 0, 2718, 2718)]

  // testing different values for thresh, round and value. just used random numbers to generate these cases
  [InlineData(74, 19, 100, 95)]
  [InlineData(39, 72, 94, 72)]
  [InlineData(29, 50, 13, 0)]
  [InlineData(7, 13, 8, 13)]
  [InlineData(84, 75, 24, 0)]
  [InlineData(86, 48, 74, 0)]
  public void Obfuscate_ReturnsCorrectResult(int threshold, int round, int value, int expected)
  {
    var options = Options.Create(new ObfuscationOptions()
      {
        LowNumberSuppressionThreshold = threshold,
        RoundingTarget = round
      }
    );

    IObfuscator obfuscator = new Obfuscator(options);

    var actual = obfuscator.Obfuscate(value);

    Assert.Equal(expected, actual);
  }

  [Theory]
  // Note that these test cases explicitly expect the defaults to be 10;
  // If the defaults change, the test is still valid but the cases must change
  [InlineData(9, 0)] // suppress first, then round
  [InlineData(10, 10)] // above suppression threshold
  [InlineData(14, 10)] // round down
  [InlineData(15, 20)] // round up

  public void Obfuscate_NoConfig_UsesNonZeroDefaults(int value, int expected)
  {
    // Default options
    var options = Options.Create(new ObfuscationOptions());
    
    // Assert that the options have initialised to the defaults
    Assert.Equal(ObfuscationDefaults.LowNumberSuppressionThreshold, options.Value.LowNumberSuppressionThreshold);
    Assert.Equal(ObfuscationDefaults.RoundingTarget, options.Value.RoundingTarget);
    
    // Assert that the defaults are non-zero (i.e. Obfuscation ON by default)
    Assert.NotEqual(0, options.Value.LowNumberSuppressionThreshold);
    Assert.NotEqual(0, options.Value.RoundingTarget);

    IObfuscator obfuscator = new Obfuscator(options);
    var actual = obfuscator.Obfuscate(value);
    
    // Assert that the default options are being applied as expected per test cases
    Assert.Equal(expected, actual);
  }
}
