using BLVK.Pickly.Logistics.Application.ReferenceData.Features.GetCountries;
using BLVK.Pickly.Logistics.Domain.Common.Caching;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.ReferenceData.GetCountries;

public sealed class When_GettingCountries_WithoutCachedResponse_Should
{
    [Fact]
    public async Task CacheAndReturnMappedCountries()
    {
        var countries = Substitute.For<IRepository<Country>>();
        var cache = Substitute.For<IJsonCache>();
        var now = new DateTimeOffset(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
        var entities = new[]
        {
            Country.Create("Nigeria", "NG", "+234", "https://example.com/ng.svg")
        };

        cache.GetAsync<GetCountriesResponse[]>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GetCountriesResponse[]?)null);
        countries.ListAsync(Arg.Any<ISpecification<Country>>(), Arg.Any<CancellationToken>())
            .Returns(entities);

        var sut = new GetCountriesHandler(countries, cache);

        var result = await sut.HandleAsync(CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].CountryId.ShouldBe(entities[0].Id);
        result[0].Name.ShouldBe("Nigeria");
        await cache.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Any<GetCountriesResponse[]>(),
            TimeSpan.FromHours(12),
            Arg.Any<CancellationToken>());
    }
}


