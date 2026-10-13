using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.Profiles;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Stakeholders.Profiles.CreateAvatarUpload;

public sealed class When_CreatingAvatarUpload_WithUnsupportedFile_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        currentActor.ActorId.Returns(Guid.CreateVersion7().ToString());
        currentActor.TenantId.Returns(Guid.CreateVersion7());
        var sut = ProfilesControllerTestFactory.Create(
            Substitute.For<IStakeholderReadModelRepository>(),
            Substitute.For<IRepository<Stakeholder>>(),
            Substitute.For<ICustomTelemetryContext>(),
            Substitute.For<IUnitOfWork>(),
            currentActor);

        var result = await sut.CreateAvatarUpload(
            new CreateAvatarUploadRequest("avatar.svg", "image/svg+xml", 128),
            CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
