using BLVK.Pickly.Logistics.Application.Stakeholders.Features.CompleteAvatarUpload;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Stakeholders.AvatarUploads;

public sealed class When_CompletingAvatarUpload_WithCompletedUpload_Should
{
    [Fact]
    public async Task ReturnPersistedUrlWithoutStorageCalls()
    {
        var context = new AvatarUploadHandlerTestContext();
        var upload = context.PendingUpload();
        upload.MarkCompleted("https://cdn.example/avatar.png", "etag-a");
        context.FileUploadSessionRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<FileUploadSession>>(), Arg.Any<CancellationToken>())
            .Returns(upload);

        var result = await context.CompleteHandler().HandleAsync(
            new CompleteAvatarUploadCommand(upload.Id, context.ActorContext()),
            CancellationToken.None);

        result.Status.ShouldBe(CompleteAvatarUploadStatus.Success);
        result.AvatarUrl.ShouldBe(upload.FinalLocation);
        await context.ObjectStorageService.DidNotReceiveWithAnyArgs()
            .PromotePrivateObjectAsync(default!, default);
    }
}
