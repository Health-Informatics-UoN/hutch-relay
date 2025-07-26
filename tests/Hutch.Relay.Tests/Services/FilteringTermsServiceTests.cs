using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services;

public class FilteringTermsServiceTests
{
  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void RequestFilteringTerms_BeaconDisabled_LogsWarningAndDoesNothing(bool isBeaconEnabled)
  {
    var logger = new Mock<ILogger<FilteringTermsService>>();

    // Ultimately we'll need a queue, or a spy on RelayTaskService? to verify that it "Does Nothing"

    var filteringTermsService = new FilteringTermsService(
      logger.Object,
      Options.Create<RelayBeaconOptions>(new()
      {
        Enable = isBeaconEnabled
      }));

    filteringTermsService.RequestFilteringTerms();

    logger.Verify(
      x => x.Log<It.IsAnyType>( // Must use logger.Log<It.IsAnyType> to sub-out FormattedLogValues, the internal class
        LogLevel.Warning, // Match whichever log level you want here
        0, // EventId
        It.Is<It.IsAnyType>((o, t) => string.Equals(
          "GA4GH Beacon Functionality is disabled; not requesting FilteringTerms.", o.ToString())), // The type here must match the `logger.Log<T>` type used above
        null, //It.IsAny<Exception>(), // Whatever exception may have been logged with it, change as needed.
        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), // The message formatter
      isBeaconEnabled ? Times.Never : Times.Once);
  }
}
