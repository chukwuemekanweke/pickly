using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Infrastructure.Authentication;
using NSubstitute;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Authentication;

public sealed class When_GettingGeolocation_WithProviderTimeout_Should
{
    [Fact]
    public async Task LogWarningAndTryNextProvider()
    {
        var firstProvider = Substitute.For<IIpGeolocationProvider>();
        var secondProvider = Substitute.For<IIpGeolocationProvider>();
        var logger = new WarningLogger<IpGeolocationService>();
        var expected = new IpGeolocation("Lagos", "Lagos", "Nigeria");
        firstProvider.GetGeolocationAsync("8.8.8.8", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IpGeolocation?>(new TaskCanceledException("Provider timed out.")));
        secondProvider.GetGeolocationAsync("8.8.8.8", Arg.Any<CancellationToken>()).Returns(expected);
        var sut = new IpGeolocationService([firstProvider, secondProvider], logger);

        var result = await sut.GetGeolocationAsync("8.8.8.8", CancellationToken.None);

        result.ShouldBe(expected);
        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].EventId.Id.ShouldBe(1);
        logger.Entries[0].Exception.ShouldBeOfType<TaskCanceledException>();
    }
}
