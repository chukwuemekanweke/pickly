using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.Profiles;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Stakeholders.Profiles;

public sealed class When_UpdatingProfile_WithValidRequest_Should
{
    [Fact]
    public async Task ReturnNoContent()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var stakeholderId = Guid.CreateVersion7();
        var stakeholder = Stakeholder.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "Initial", "User");
        var request = new UpdateProfileRequest("Jane", "Doe");

        currentActor.ActorId.Returns(stakeholderId.ToString());
        stakeholderRepository.GetByIdAsync(stakeholderId, Arg.Any<CancellationToken>()).Returns(stakeholder);

        var sut = ProfilesControllerTestFactory.Create(
            Substitute.For<IStakeholderReadModelRepository>(),
            stakeholderRepository,
            customTelemetryContext,
            unitOfWork,
            currentActor);

        var result = await sut.UpdateProfile(request, CancellationToken.None);

        result.ShouldBeOfType<NoContentResult>();
    }
}


