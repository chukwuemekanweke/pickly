using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Jobs.Authentication;
using BLVK.Pickly.Logistics.Jobs.Infrastructure.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Jobs.UnitTests.Authentication.IpAddressLocationEnrichment;

public sealed class When_EnrichingIpAddresses_WithNoPendingRecords_Should
{
    [Fact]
    public async Task SkipSavingChanges()
    {
        var repository = Substitute.For<IRepository<IpAddress>>();
        var geolocationService = Substitute.For<IIpGeolocationService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var state = new BackgroundServiceReadinessState([new BackgroundServiceDescriptor(IpAddressLocationEnrichmentProcessor.ServiceName)]);
        var services = new ServiceCollection()
            .AddSingleton(repository)
            .AddSingleton(geolocationService)
            .AddSingleton(unitOfWork)
            .BuildServiceProvider();

        repository.ListAsync(Arg.Any<ISpecification<IpAddress>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IpAddress>());

        var sut = new IpAddressLocationEnrichmentProcessor(
            services.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            Options.Create(new IpAddressLocationEnrichmentOptions { BatchSize = 10, PollIntervalSeconds = 1, LocationRefreshIntervalDays = 30 }),
            state,
            NullLogger<IpAddressLocationEnrichmentProcessor>.Instance);

        await sut.StartAsync(CancellationToken.None);
        await Task.Delay(150);
        await sut.StopAsync(CancellationToken.None);

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
