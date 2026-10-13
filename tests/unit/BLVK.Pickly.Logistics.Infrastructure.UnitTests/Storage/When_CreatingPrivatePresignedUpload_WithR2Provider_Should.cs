using Amazon.S3;
using Amazon.S3.Model;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using NSubstitute;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Storage;

public sealed class When_CreatingPrivatePresignedUpload_WithR2Provider_Should
{
    [Fact]
    public async Task SignScopedPutWithContentTypeAndExpiration()
    {
        var context = new CloudflareR2ProviderTestContext();
        GetPreSignedUrlRequest? capturedRequest = null;
        context.Client.GetPreSignedURLAsync(Arg.Do<GetPreSignedUrlRequest>(request => capturedRequest = request))
            .Returns("https://account.r2.cloudflarestorage.com/private-bucket/key?signature=secret");
        var expiresAtUtc = new DateTimeOffset(2026, 8, 16, 13, 50, 0, TimeSpan.Zero);

        var result = await context.CreateProvider().CreatePrivatePresignedUploadAsync(
            new ObjectStoragePresignedUploadRequest("quarantine/avatars/upload.jpg", "image/jpeg", expiresAtUtc),
            CancellationToken.None);

        capturedRequest.ShouldNotBeNull();
        capturedRequest.BucketName.ShouldBe("private-bucket");
        capturedRequest.Key.ShouldBe("backend-template/quarantine/avatars/upload.jpg");
        capturedRequest.Verb.ShouldBe(HttpVerb.PUT);
        capturedRequest.ContentType.ShouldBe("image/jpeg");
        capturedRequest.Expires.ShouldBe(expiresAtUtc.UtcDateTime);
        result.Headers["Content-Type"].ShouldBe("image/jpeg");
    }
}
