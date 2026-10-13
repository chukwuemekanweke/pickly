using BLVK.Pickly.Logistics.Application.Stakeholders.Features.CompleteAvatarUpload;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Stakeholders.AvatarUploads;

public sealed class When_CompletingAvatarUpload_AfterObjectChanged_Should
{
    [Fact]
    public async Task ReturnConflictStateWithoutUpdatingAvatar()
    {
        var context = new AvatarUploadHandlerTestContext();
        var upload = context.PendingUpload();
        context.FileUploadSessionRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<FileUploadSession>>(), Arg.Any<CancellationToken>())
            .Returns(upload);
        context.ObjectStorageService.GetPrivateObjectMetadataAsync(upload.QuarantineObjectKey, Arg.Any<CancellationToken>())
            .Returns(new ObjectStorageObjectMetadata(12, "image/png", "etag-a"));
        context.ObjectStorageService.ReadPrivateObjectRangeAsync(Arg.Any<ObjectStorageRangeReadRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<byte[]>>(_ => throw new ObjectStoragePreconditionFailedException("changed"));

        var result = await context.CompleteHandler().HandleAsync(
            new CompleteAvatarUploadCommand(upload.Id, context.ActorContext()),
            CancellationToken.None);

        result.Status.ShouldBe(CompleteAvatarUploadStatus.UploadChanged);
        context.Stakeholder.AvatarUrl.ShouldBeNull();
        upload.Status.ShouldBe(FileUploadStatus.Rejected);
    }
}
