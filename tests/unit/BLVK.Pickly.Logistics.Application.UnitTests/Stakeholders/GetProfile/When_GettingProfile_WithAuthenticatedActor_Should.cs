using BLVK.Pickly.Logistics.Application.Stakeholders.Features.GetProfile;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Stakeholders.GetProfile;

public sealed class When_GettingProfile_WithAuthenticatedActor_Should
{
    [Fact]
    public async Task ReturnMappedProfile()
    {
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
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
        stakeholderReadModelRepository
            .GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        var sut = new GetProfileHandler(stakeholderReadModelRepository);

        var result = await sut.HandleAsync(
            new GetProfileQuery(CreateActorContext(stakeholderId, tenantId)),
            CancellationToken.None);

        result.Status.ShouldBe(GetProfileStatus.Success);
        result.Profile.ShouldBe(new GetProfileResponse(
            stakeholderId,
            stakeholder.EmailAddress,
            stakeholder.FirstName,
            stakeholder.LastName,
            stakeholder.AvatarUrl,
            stakeholder.IsVerified));
    }

    private static ActorContext CreateActorContext(Guid stakeholderId, Guid tenantId) =>
        new(stakeholderId, tenantId, Guid.CreateVersion7().ToString("N"), Guid.CreateVersion7().ToString("N"));
}
