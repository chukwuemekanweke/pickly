using BLVK.Pickly.Logistics.Application.ReferenceData.Features.GetCountries;
using BLVK.Pickly.Logistics.Domain.Common.Caching;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.ReferenceData.GetCountries;

public sealed class When_GettingCountries_WithCachedResponseAvailable_Should
{
    [Fact]
    public async Task ReturnCachedCountries()
    {
        var countries = Substitute.For<IRepository<Country>>();
        var cache = Substitute.For<IJsonCache>();
        var countryId = Guid.CreateVersion7();
        var cachedResponse = new[]
        {
            new GetCountriesResponse(countryId, "Nigeria", "NG", "+234", "https://example.com/ng.svg")
        };

        cache.GetAsync<GetCountriesResponse[]>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(cachedResponse);

        var sut = new GetCountriesHandler(countries, cache);

        var result = await sut.HandleAsync(CancellationToken.None);

        result.ShouldBe(cachedResponse);
        result[0].CountryId.ShouldBe(countryId);
        await countries.DidNotReceiveWithAnyArgs().ListAsync(default!, default);
    }
}
