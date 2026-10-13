using BLVK.Pickly.Logistics.Application.Providers.Features.ActivateProvider;
using BLVK.Pickly.Logistics.Domain.Providers.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Providers.ActivateProvider;

public sealed class When_ActivatingProvider_WithMatchingProviderKey_Should
{
    [Fact]
    public async Task MarkSelectedProviderActive()
    {
        var providerRepository = Substitute.For<IRepository<Provider>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var activeProvider = Provider.Create(ProviderType.Email, "Primary", "primary", true);
        var inactiveProvider = Provider.Create(ProviderType.Email, "Secondary", "secondary", false);

        providerRepository.ListAsync(Arg.Any<ISpecification<Provider>>(), Arg.Any<CancellationToken>())
            .Returns([activeProvider, inactiveProvider]);

        var sut = new ActivateProviderHandler(providerRepository, unitOfWork);

        var result = await sut.HandleAsync(
            new ActivateProviderCommand(ProviderType.Email, "secondary"),
            CancellationToken.None);

        result.Status.ShouldBe(ActivateProviderStatus.Success);
        activeProvider.IsActive.ShouldBeFalse();
        inactiveProvider.IsActive.ShouldBeTrue();
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

}


