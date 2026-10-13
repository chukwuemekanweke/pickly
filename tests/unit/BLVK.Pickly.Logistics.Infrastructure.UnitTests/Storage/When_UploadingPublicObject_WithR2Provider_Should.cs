using Amazon.S3.Model;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using NSubstitute;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Storage;

public sealed class When_UploadingPublicObject_WithR2Provider_Should
{
    [Fact]
    public async Task UploadToPublicBucketAndReturnPublicBaseUrl()
    {
        var context = new CloudflareR2ProviderTestContext();
        PutObjectRequest? capturedRequest = null;
        context.Client.PutObjectAsync(
                Arg.Do<PutObjectRequest>(request => capturedRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse());
        await using var content = new MemoryStream([1, 2, 3]);

        var result = await context.CreateProvider().UploadPublicAsync(
            new ObjectStorageUploadRequest("avatars/upload.webp", content, "image/webp"),
            CancellationToken.None);

        capturedRequest.ShouldNotBeNull();
        capturedRequest.BucketName.ShouldBe("public-bucket");
        capturedRequest.Key.ShouldBe("backend-template/avatars/upload.webp");
        result.ShouldBe("https://cdn.example.com/backend-template/avatars/upload.webp");
    }
}
