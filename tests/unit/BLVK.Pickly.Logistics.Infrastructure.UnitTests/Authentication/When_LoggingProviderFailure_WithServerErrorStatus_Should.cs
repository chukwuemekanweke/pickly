using BLVK.Pickly.Logistics.Infrastructure.Authentication;
using Shouldly;
using System.Net;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Authentication;

public sealed class When_LoggingProviderFailure_WithServerErrorStatus_Should
{
    [Fact]
    public void LogWarning()
    {
        var logger = new WarningLogger<IpInfoClient>();

        IpGeolocationLog.ProviderReturnedFailure(logger, nameof(IpInfoClient), HttpStatusCode.ServiceUnavailable);

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].EventId.Id.ShouldBe(5);
        logger.Entries[0].Message.ShouldContain("status code 503");
    }
}
