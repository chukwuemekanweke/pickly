using BLVK.Pickly.Logistics.Application.Stakeholders.Features.UpdateProfile;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Stakeholders.UpdateProfile;

public sealed class When_UpdatingProfile_WithValidActorAndPayload_Should
{
    [Fact]
    public async Task PersistProfileChanges()
    {
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var stakeholderId = Guid.CreateVersion7();
        var stakeholder = Stakeholder.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "Initial", "User");

        stakeholderRepository.GetByIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(stakeholder);

        var sut = new UpdateProfileHandler(stakeholderRepository, customTelemetryContext, unitOfWork);

        var result = await sut.HandleAsync(
            new UpdateProfileCommand("Jane", "Doe", new ActorContext(stakeholderId, Guid.CreateVersion7(), Guid.CreateVersion7().ToString("N"), Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        result.Status.ShouldBe(UpdateProfileStatus.Success);
        stakeholder.FirstName.ShouldBe("Jane");
        stakeholder.LastName.ShouldBe("Doe");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}


