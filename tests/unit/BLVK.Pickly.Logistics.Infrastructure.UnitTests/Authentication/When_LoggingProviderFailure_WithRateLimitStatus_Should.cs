using BLVK.Pickly.Logistics.Infrastructure.Authentication;
using Shouldly;
using System.Net;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Authentication;

public sealed class When_LoggingProviderFailure_WithRateLimitStatus_Should
{
    [Fact]
    public void LogWarning()
    {
        var logger = new WarningLogger<IpInfoClient>();

        IpGeolocationLog.ProviderReturnedFailure(logger, nameof(IpInfoClient), HttpStatusCode.TooManyRequests);

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].EventId.Id.ShouldBe(4);
        logger.Entries[0].Message.ShouldContain("status code 429");
    }
}
