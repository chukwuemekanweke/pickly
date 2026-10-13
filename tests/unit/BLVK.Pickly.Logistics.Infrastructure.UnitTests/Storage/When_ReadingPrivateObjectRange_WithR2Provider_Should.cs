using Amazon.S3.Model;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using NSubstitute;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Storage;

public sealed class When_ReadingPrivateObjectRange_WithR2Provider_Should
{
    [Fact]
    public async Task UsePrivateBucketRangeAndExpectedETag()
    {
        var context = new CloudflareR2ProviderTestContext();
        GetObjectRequest? capturedRequest = null;
        context.Client.GetObjectAsync(Arg.Do<GetObjectRequest>(request => capturedRequest = request), Arg.Any<CancellationToken>())
            .Returns(new GetObjectResponse { ResponseStream = new MemoryStream([1, 2, 3]) });

        var result = await context.CreateProvider().ReadPrivateObjectRangeAsync(
            new ObjectStorageRangeReadRequest("quarantine/upload.png", 0, 11, "\"etag-a\""),
            CancellationToken.None);

        result.ShouldBe(new byte[] { 1, 2, 3 });
        capturedRequest.ShouldNotBeNull();
        capturedRequest.BucketName.ShouldBe("private-bucket");
        capturedRequest.Key.ShouldBe("backend-template/quarantine/upload.png");
        capturedRequest.ByteRange.Start.ShouldBe(0);
        capturedRequest.ByteRange.End.ShouldBe(11);
        capturedRequest.EtagToMatch.ShouldBe("\"etag-a\"");
    }
}
