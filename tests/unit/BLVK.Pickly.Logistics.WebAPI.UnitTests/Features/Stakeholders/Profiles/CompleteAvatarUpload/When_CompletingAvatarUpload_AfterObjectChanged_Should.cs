using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Stakeholders.Profiles.CompleteAvatarUpload;

public sealed class When_CompletingAvatarUpload_AfterObjectChanged_Should
{
    [Fact]
    public async Task ReturnConflict()
    {
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var currentActor = Substitute.For<ICurrentActor>();
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var uploadRepository = Substitute.For<IRepository<FileUploadSession>>();
        var objectStorageService = Substitute.For<IObjectStorageService>();
        var stakeholder = Stakeholder.Create(
            Guid.CreateVersion7(), tenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), "Jane", "Doe");
        var uploadId = Guid.CreateVersion7();
        var upload = FileUploadSession.Create(
            uploadId,
            tenantId,
            "stakeholder",
            stakeholderId,
            stakeholderId,
            "stakeholder-profile-avatar",
            "stakeholder-avatar-v1",
            "avatar.png",
            "image/png",
            12,
            ".png",
            "quarantine/avatar.png",
            "avatar/avatar.png",
            ObjectStorageVisibility.Public,
            TimeProvider.System.GetUtcNow().AddMinutes(10));
        currentActor.ActorId.Returns(stakeholderId.ToString());
        currentActor.TenantId.Returns(tenantId);
        stakeholderRepository.GetByIdAsync(stakeholderId, Arg.Any<CancellationToken>()).Returns(stakeholder);
        uploadRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<FileUploadSession>>(), Arg.Any<CancellationToken>()).Returns(upload);
        objectStorageService.GetPrivateObjectMetadataAsync(upload.QuarantineObjectKey, Arg.Any<CancellationToken>())
            .Returns(new ObjectStorageObjectMetadata(12, "image/png", "etag-a"));
        objectStorageService.ReadPrivateObjectRangeAsync(Arg.Any<ObjectStorageRangeReadRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<byte[]>>(_ => throw new ObjectStoragePreconditionFailedException("changed"));
        var sut = ProfilesControllerTestFactory.Create(
            Substitute.For<IStakeholderReadModelRepository>(),
            stakeholderRepository,
            Substitute.For<ICustomTelemetryContext>(),
            Substitute.For<IUnitOfWork>(),
            currentActor,
            objectStorageService,
            uploadRepository);

        var result = await sut.CompleteAvatarUpload(uploadId, CancellationToken.None);

        result.Result.ShouldBeOfType<ConflictObjectResult>();
    }
}
