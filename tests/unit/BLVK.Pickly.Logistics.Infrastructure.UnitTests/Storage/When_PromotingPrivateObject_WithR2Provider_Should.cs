using Amazon.S3.Model;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using NSubstitute;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Storage;

public sealed class When_PromotingPrivateObject_WithR2Provider_Should
{
    [Fact]
    public async Task ConditionallyCopyFromPrivateToPublicAndReturnCdnUrl()
    {
        var context = new CloudflareR2ProviderTestContext();
        CopyObjectRequest? capturedRequest = null;
        context.Client.CopyObjectAsync(Arg.Do<CopyObjectRequest>(request => capturedRequest = request), Arg.Any<CancellationToken>())
            .Returns(new CopyObjectResponse());

        var result = await context.CreateProvider().PromotePrivateObjectAsync(
            new ObjectStoragePromotionRequest(
                "backend-template/quarantine/upload.webp",
                "avatars/upload.webp",
                "\"etag-a\"",
                "image/webp",
                ObjectStorageVisibility.Public),
            CancellationToken.None);

        capturedRequest.ShouldNotBeNull();
        capturedRequest.SourceBucket.ShouldBe("private-bucket");
        capturedRequest.SourceKey.ShouldBe("backend-template/quarantine/upload.webp");
        capturedRequest.DestinationBucket.ShouldBe("public-bucket");
        capturedRequest.DestinationKey.ShouldBe("backend-template/avatars/upload.webp");
        capturedRequest.ETagToMatch.ShouldBe("\"etag-a\"");
        capturedRequest.ContentType.ShouldBe("image/webp");
        result.ShouldBe("https://cdn.example.com/backend-template/avatars/upload.webp");
    }
}
