using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Locations;

public sealed partial class When_ManagingSharedLocations_WithPostgis_Should
{
[Fact]
    public async Task CallProviderOnceForConcurrentResolutionOfTheSameAddress()
    {
        LocationGeocodingProvider.Responder = async (request, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(600), cancellationToken);
            return Success(Candidate($"{_run}-addr-30", request.AddressLine));
        };
        var query = $"30 Concurrent Crescent {_run}";

        var results = await Task.WhenAll(SearchAsync(query), SearchAsync(query), SearchAsync(query));

        results.ShouldAllBe(result => result.Items.Any(item => item.SelectionToken != null));
        LocationGeocodingProvider.CallCount.ShouldBe(1);
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await dbContext.LocationProviderReferences.CountAsync(reference => reference.FeatureId == $"{_run}-addr-30")).ShouldBe(0);
    }
}
