using BLVK.Pickly.Logistics.Application.Stakeholders.Features.CompleteAvatarUpload;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Stakeholders.AvatarUploads;

public sealed class When_CompletingAvatarUpload_WithInvalidMetadata_Should
{
    [Theory]
    [InlineData(0, "image/png")]
    [InlineData(2097153, "image/png")]
    [InlineData(11, "image/png")]
    [InlineData(12, "application/octet-stream")]
    public async Task RejectWithoutPromotion(long length, string contentType)
    {
        var context = new AvatarUploadHandlerTestContext();
        var upload = context.PendingUpload();
        context.FileUploadSessionRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<FileUploadSession>>(), Arg.Any<CancellationToken>())
            .Returns(upload);
        context.ObjectStorageService.GetPrivateObjectMetadataAsync(upload.QuarantineObjectKey, Arg.Any<CancellationToken>())
            .Returns(new ObjectStorageObjectMetadata(length, contentType, "etag-a"));

        var result = await context.CompleteHandler().HandleAsync(
            new CompleteAvatarUploadCommand(upload.Id, context.ActorContext()),
            CancellationToken.None);

        result.Status.ShouldBe(CompleteAvatarUploadStatus.InvalidFile);
        upload.Status.ShouldBe(FileUploadStatus.Rejected);
        context.Stakeholder.AvatarUrl.ShouldBeNull();
        await context.ObjectStorageService.DidNotReceiveWithAnyArgs()
            .PromotePrivateObjectAsync(default!, default);
    }
}
