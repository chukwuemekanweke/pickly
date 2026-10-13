using BLVK.Pickly.Logistics.Application.Stakeholders.Features.GetProfile;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Stakeholders.GetProfile;

public sealed class When_GettingProfile_WithInvalidStakeholderId_Should
{
    [Fact]
    public async Task ReturnNotAuthenticated()
    {
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var actorContext = new ActorContext(
            Guid.Empty,
            Guid.CreateVersion7(),
            Guid.CreateVersion7().ToString("N"),
            Guid.CreateVersion7().ToString("N"));
        var sut = new GetProfileHandler(stakeholderReadModelRepository);

        var result = await sut.HandleAsync(
            new GetProfileQuery(actorContext),
            CancellationToken.None);

        result.Status.ShouldBe(GetProfileStatus.NotAuthenticated);
        result.Profile.ShouldBeNull();
        await stakeholderReadModelRepository.DidNotReceiveWithAnyArgs()
            .GetByStakeholderIdAsync(default, default);
    }
}
