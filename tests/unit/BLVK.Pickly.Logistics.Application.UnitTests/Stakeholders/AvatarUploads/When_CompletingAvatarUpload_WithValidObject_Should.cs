using BLVK.Pickly.Logistics.Application.Stakeholders.Features.CompleteAvatarUpload;
using BLVK.Pickly.Logistics.Contracts.Commands.Storage;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Stakeholders.AvatarUploads;

public sealed class When_CompletingAvatarUpload_WithValidObject_Should
{
    [Fact]
    public async Task PromotePersistAndQueueQuarantineDeletion()
    {
        var context = new AvatarUploadHandlerTestContext();
        var upload = context.PendingUpload();
        context.FileUploadSessionRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<FileUploadSession>>(), Arg.Any<CancellationToken>())
            .Returns(upload);
        context.ObjectStorageService.GetPrivateObjectMetadataAsync(upload.QuarantineObjectKey, Arg.Any<CancellationToken>())
            .Returns(new ObjectStorageObjectMetadata(12, "image/png", "etag-a"));
        context.ObjectStorageService.ReadPrivateObjectRangeAsync(Arg.Any<ObjectStorageRangeReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 });
        context.ObjectStorageService.PromotePrivateObjectAsync(Arg.Any<ObjectStoragePromotionRequest>(), Arg.Any<CancellationToken>())
            .Returns("https://cdn.example/avatar.png");

        var result = await context.CompleteHandler().HandleAsync(
            new CompleteAvatarUploadCommand(upload.Id, context.ActorContext()),
            CancellationToken.None);

        result.Status.ShouldBe(CompleteAvatarUploadStatus.Success);
        context.Stakeholder.AvatarUrl.ShouldBe("https://cdn.example/avatar.png");
        upload.Status.ShouldBe(FileUploadStatus.Completed);
        upload.FinalLocation.ShouldBe(result.AvatarUrl);
        upload.ValidatedETag.ShouldBe("etag-a");
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await context.CommandSender.Received(1).SendAsync(
            Arg.Is<DeleteQuarantinedObject>(command =>
                command.UploadId == upload.Id &&
                command.ObjectKey == upload.QuarantineObjectKey &&
                command.StakeholderId == upload.OwnerId &&
                command.TenantId == upload.TenantId),
            Arg.Any<CancellationToken>());
    }
}
