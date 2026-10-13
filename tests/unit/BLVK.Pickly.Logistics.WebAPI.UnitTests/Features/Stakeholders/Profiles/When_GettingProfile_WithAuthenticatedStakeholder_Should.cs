using BLVK.Pickly.Logistics.Application.Stakeholders.Features.GetProfile;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.Profiles;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Stakeholders.Profiles;

public sealed class When_GettingProfile_WithAuthenticatedStakeholder_Should
{
    [Fact]
    public async Task ReturnProfile()
    {
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var currentActor = Substitute.For<ICurrentActor>();
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var stakeholder = new StakeholderReadModel(
            stakeholderId,
            Guid.CreateVersion7(),
            "ada@example.com",
            tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Ada",
            "Lovelace",
            "https://cdn.example.com/avatars/ada.jpg",
            true);
        currentActor.ActorId.Returns(stakeholderId.ToString());
        currentActor.TenantId.Returns(tenantId);
        stakeholderReadModelRepository
            .GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        var sut = CreateController(stakeholderReadModelRepository, currentActor);

        var result = await sut.GetProfile(CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(new GetProfileResponse(
            stakeholderId,
            stakeholder.EmailAddress,
            stakeholder.FirstName,
            stakeholder.LastName,
            stakeholder.AvatarUrl,
            stakeholder.IsVerified));
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
