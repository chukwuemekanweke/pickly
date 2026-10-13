using BLVK.Pickly.Logistics.Application.Stakeholders.Features.GetProfile;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Stakeholders.GetProfile;

public sealed class When_GettingProfile_ForCrossTenantStakeholder_Should
{
    [Fact]
    public async Task ReturnStakeholderNotFound()
    {
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var stakeholderId = Guid.CreateVersion7();
        var stakeholder = new StakeholderReadModel(
            stakeholderId,
            Guid.CreateVersion7(),
            "ada@example.com",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Ada",
            "Lovelace",
            null,
            true);
        stakeholderReadModelRepository
            .GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        var sut = new GetProfileHandler(stakeholderReadModelRepository);

        var result = await sut.HandleAsync(
            new GetProfileQuery(new ActorContext(
                stakeholderId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7().ToString("N"),
                Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        result.Status.ShouldBe(GetProfileStatus.StakeholderNotFound);
        result.Profile.ShouldBeNull();
    }
}
