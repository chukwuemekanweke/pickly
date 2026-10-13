using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.Profiles;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Stakeholders.Profiles.CreateAvatarUpload;

public sealed class When_CreatingAvatarUpload_WithValidRequest_Should
{
    [Fact]
    public async Task ReturnUploadInstructions()
    {
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var currentActor = Substitute.For<ICurrentActor>();
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var objectStorageService = Substitute.For<IObjectStorageService>();
        var stakeholder = Stakeholder.Create(
            Guid.CreateVersion7(), tenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), "Jane", "Doe");
        currentActor.ActorId.Returns(stakeholderId.ToString());
        currentActor.TenantId.Returns(tenantId);
        stakeholderRepository.GetByIdAsync(stakeholderId, Arg.Any<CancellationToken>()).Returns(stakeholder);
        objectStorageService.CreatePrivatePresignedUploadAsync(Arg.Any<ObjectStoragePresignedUploadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => new ObjectStoragePresignedUploadResult(
                "https://r2.example/upload?signature=secret",
                new Dictionary<string, string> { ["Content-Type"] = "image/png" },
                call.Arg<ObjectStoragePresignedUploadRequest>().ExpiresAtUtc));
        var sut = ProfilesControllerTestFactory.Create(
            Substitute.For<IStakeholderReadModelRepository>(),
            stakeholderRepository,
            Substitute.For<ICustomTelemetryContext>(),
            Substitute.For<IUnitOfWork>(),
            currentActor,
            objectStorageService);

        var result = await sut.CreateAvatarUpload(
            new CreateAvatarUploadRequest("avatar.png", "image/png", 128),
            CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CreateAvatarUploadResponse>();
        response.Method.ShouldBe("PUT");
        response.Headers["Content-Type"].ShouldBe("image/png");
    }
}
