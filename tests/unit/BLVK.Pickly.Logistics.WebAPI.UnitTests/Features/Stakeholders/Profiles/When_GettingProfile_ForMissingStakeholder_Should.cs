using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.Profiles;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Stakeholders.Profiles;

public sealed class When_GettingProfile_ForMissingStakeholder_Should
{
    [Fact]
    public async Task ReturnNotFound()
    {
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var currentActor = Substitute.For<ICurrentActor>();
        var stakeholderId = Guid.CreateVersion7();
        currentActor.ActorId.Returns(stakeholderId.ToString());
        currentActor.TenantId.Returns(Guid.CreateVersion7());
        stakeholderReadModelRepository
            .GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns((StakeholderReadModel?)null);
        var sut = CreateController(stakeholderReadModelRepository, currentActor);

        var result = await sut.GetProfile(CancellationToken.None);

        result.Result.ShouldBeOfType<NotFoundResult>();
    }

    private static ProfilesController CreateController(
        IStakeholderReadModelRepository stakeholderReadModelRepository,
        ICurrentActor currentActor)
    {
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        return ProfilesControllerTestFactory.Create(
            stakeholderReadModelRepository,
            stakeholderRepository,
            customTelemetryContext,
            unitOfWork,
            currentActor);
    }
}
