using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.Profiles;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Stakeholders.Profiles;

public sealed class When_UpdatingProfile_WithMissingNames_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        currentActor.ActorId.Returns(Guid.CreateVersion7().ToString());

        var sut = ProfilesControllerTestFactory.Create(
            Substitute.For<IStakeholderReadModelRepository>(),
            Substitute.For<IRepository<Stakeholder>>(),
            customTelemetryContext,
            Substitute.For<IUnitOfWork>(),
            currentActor);

        var result = await sut.UpdateProfile(new UpdateProfileRequest(string.Empty, string.Empty), CancellationToken.None);

        result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
