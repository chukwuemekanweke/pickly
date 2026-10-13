using BLVK.Pickly.Logistics.Application.Common.FileUploads;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Common.FileUploads;

public sealed class When_CompletingFileUpload_WithPrivatePolicy_Should
{
    [Fact]
    public async Task ValidateAndPromoteToPrivateDestination()
    {
        var storage = Substitute.For<IObjectStorageService>();
        storage.GetPrivateObjectMetadataAsync("quarantine/document.pdf", Arg.Any<CancellationToken>())
            .Returns(new ObjectStorageObjectMetadata(5, "application/pdf", "etag-a"));
        storage.ReadPrivateObjectRangeAsync(Arg.Any<ObjectStorageRangeReadRequest>(), Arg.Any<CancellationToken>())
            .Returns("%PDF-"u8.ToArray());
        storage.PromotePrivateObjectAsync(Arg.Any<ObjectStoragePromotionRequest>(), Arg.Any<CancellationToken>())
            .Returns("https://storage.example/private/documents/document.pdf");
        var service = new FileUploadService(storage);

        var result = await service.CompleteAsync(
            new FileUploadCompletionRequest(
                "quarantine/document.pdf",
                "documents/document.pdf",
                "application/pdf",
                5,
                ObjectStorageVisibility.Private),
            new TestDocumentUploadPolicy(),
            CancellationToken.None);

        result.Status.ShouldBe(FileUploadCompletionStatus.Success);
        result.ValidatedETag.ShouldBe("etag-a");
        await storage.Received(1).PromotePrivateObjectAsync(
            Arg.Is<ObjectStoragePromotionRequest>(request =>
                request.DestinationObjectKey == "documents/document.pdf" &&
                request.DestinationVisibility == ObjectStorageVisibility.Private),
            Arg.Any<CancellationToken>());
    }
}
