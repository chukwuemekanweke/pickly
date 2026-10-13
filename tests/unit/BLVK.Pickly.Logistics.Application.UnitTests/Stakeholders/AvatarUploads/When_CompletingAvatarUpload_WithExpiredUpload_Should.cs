using BLVK.Pickly.Logistics.Application.Stakeholders.Features.CompleteAvatarUpload;
using BLVK.Pickly.Logistics.Contracts.Commands.Storage;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Stakeholders.AvatarUploads;

public sealed class When_CompletingAvatarUpload_WithExpiredUpload_Should
{
    [Fact]
    public async Task MarkExpiredAndQueueQuarantineDeletion()
    {
        var context = new AvatarUploadHandlerTestContext();
        var upload = context.PendingUpload(expiresAtUtc: context.UtcNow.AddSeconds(-1));
        context.FileUploadSessionRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<FileUploadSession>>(), Arg.Any<CancellationToken>())
            .Returns(upload);

        var result = await context.CompleteHandler().HandleAsync(
            new CompleteAvatarUploadCommand(upload.Id, context.ActorContext()),
            CancellationToken.None);

        result.Status.ShouldBe(CompleteAvatarUploadStatus.Expired);
        upload.Status.ShouldBe(FileUploadStatus.Expired);
        await context.CommandSender.Received(1).SendAsync(
            Arg.Is<DeleteQuarantinedObject>(command => command.ObjectKey == upload.QuarantineObjectKey),
            Arg.Any<CancellationToken>());
    }
}
