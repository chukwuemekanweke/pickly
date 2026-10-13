using BLVK.Pickly.Logistics.Application.Stakeholders.Features.CompleteAvatarUpload;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Stakeholders.AvatarUploads;

public sealed class When_CompletingAvatarUpload_WithMismatchedSignature_Should
{
    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    public async Task RejectWithoutPromotion(string contentType)
    {
        var context = new AvatarUploadHandlerTestContext();
        var upload = context.PendingUpload(contentType);
        context.FileUploadSessionRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<FileUploadSession>>(), Arg.Any<CancellationToken>())
            .Returns(upload);
        context.ObjectStorageService.GetPrivateObjectMetadataAsync(upload.QuarantineObjectKey, Arg.Any<CancellationToken>())
            .Returns(new ObjectStorageObjectMetadata(12, contentType, "etag-a"));
        context.ObjectStorageService.ReadPrivateObjectRangeAsync(Arg.Any<ObjectStorageRangeReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(new byte[12]);

        var result = await context.CompleteHandler().HandleAsync(
            new CompleteAvatarUploadCommand(upload.Id, context.ActorContext()),
            CancellationToken.None);

        result.Status.ShouldBe(CompleteAvatarUploadStatus.InvalidFile);
        upload.Status.ShouldBe(FileUploadStatus.Rejected);
        await context.ObjectStorageService.DidNotReceiveWithAnyArgs()
            .PromotePrivateObjectAsync(default!, default);
    }
}
