using BLVK.Pickly.Logistics.Application.ReferenceData.Features.GetCountries;
using BLVK.Pickly.Logistics.Domain.Common.Caching;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.ReferenceData.Countries;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.ReferenceData.Countries;

public sealed class When_GettingCountries_WithEnabledCountriesAvailable_Should
{
    [Fact]
    public async Task ReturnCountries()
    {
        var repository = Substitute.For<IRepository<Country>>();
        var cache = Substitute.For<IJsonCache>();
        var countryId = Guid.CreateVersion7();
        var response = new[]
        {
            new GetCountriesResponse(countryId, "Nigeria", "NG", "+234", "https://example.com/ng.svg")
        };

        cache.GetAsync<GetCountriesResponse[]>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var sut = new CountriesController(new GetCountriesHandler(repository, cache));

        var result = await sut.Handle(CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(response);
        response[0].CountryId.ShouldBe(countryId);
    }
}
