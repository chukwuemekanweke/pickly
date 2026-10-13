using BLVK.Pickly.Logistics.Application.Stakeholders.Features.CompleteAvatarUpload;
using BLVK.Pickly.Logistics.Contracts.Commands.Storage;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Stakeholders.AvatarUploads;

public sealed class When_CompletingAvatarUpload_WithCleanupCommandFailure_Should
{
    [Fact]
    public async Task NotCommitCompletedUpload()
    {
        var context = new AvatarUploadHandlerTestContext();
        var upload = context.PendingUpload();
        context.FileUploadSessionRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<FileUploadSession>>(), Arg.Any<CancellationToken>()).Returns(upload);
        context.ObjectStorageService.GetPrivateObjectMetadataAsync(upload.QuarantineObjectKey, Arg.Any<CancellationToken>())
            .Returns(new ObjectStorageObjectMetadata(12, "image/png", "etag-a"));
        context.ObjectStorageService.ReadPrivateObjectRangeAsync(Arg.Any<ObjectStorageRangeReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 });
        context.ObjectStorageService.PromotePrivateObjectAsync(Arg.Any<ObjectStoragePromotionRequest>(), Arg.Any<CancellationToken>())
            .Returns("https://cdn.example/avatar.png");
        context.CommandSender.SendAsync(Arg.Any<DeleteQuarantinedObject>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("outbox unavailable"));

        var action = () => context.CompleteHandler().HandleAsync(
            new CompleteAvatarUploadCommand(upload.Id, context.ActorContext()),
            CancellationToken.None);

        await action.ShouldThrowAsync<InvalidOperationException>();
        await context.UnitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
