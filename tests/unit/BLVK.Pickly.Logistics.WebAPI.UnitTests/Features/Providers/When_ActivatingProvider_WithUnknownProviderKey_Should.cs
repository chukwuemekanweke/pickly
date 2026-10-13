using BLVK.Pickly.Logistics.Application.Providers.Features.ActivateProvider;
using BLVK.Pickly.Logistics.Domain.Providers.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Providers;

public sealed class When_ActivatingProvider_WithUnknownProviderKey_Should
{
    [Fact]
    public async Task ReturnNotFound()
    {
        var repository = Substitute.For<IRepository<Provider>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var request = new ActivateProviderRequest("Email", "missing");

        repository.ListAsync(Arg.Any<ISpecification<Provider>>(), Arg.Any<CancellationToken>())
            .Returns([
                Provider.Create(ProviderType.Email, "Primary", "primary", true)
            ]);

        var sut = new ProvidersController(
            new ActivateProviderHandler(repository, unitOfWork),
            new ActivateProviderValidator());

        var result = await sut.ActivateProvider(request, CancellationToken.None);

        var objectResult = result.ShouldBeOfType<NotFoundObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }
}


