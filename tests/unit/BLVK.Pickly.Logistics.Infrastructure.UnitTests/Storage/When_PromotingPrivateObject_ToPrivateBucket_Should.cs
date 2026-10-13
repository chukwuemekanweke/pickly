using Amazon.S3.Model;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using NSubstitute;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Storage;

public sealed class When_PromotingPrivateObject_ToPrivateBucket_Should
{
    [Fact]
    public async Task ConditionallyCopyWithinPrivateStorage()
    {
        var context = new CloudflareR2ProviderTestContext();
        CopyObjectRequest? capturedRequest = null;
        context.Client.CopyObjectAsync(Arg.Do<CopyObjectRequest>(request => capturedRequest = request), Arg.Any<CancellationToken>())
            .Returns(new CopyObjectResponse());

        var result = await context.CreateProvider().PromotePrivateObjectAsync(
            new ObjectStoragePromotionRequest(
                "quarantine/documents/document.pdf",
                "documents/document.pdf",
                "\"etag-a\"",
                "application/pdf",
                ObjectStorageVisibility.Private),
            CancellationToken.None);

        capturedRequest.ShouldNotBeNull();
        capturedRequest.SourceBucket.ShouldBe("private-bucket");
        capturedRequest.DestinationBucket.ShouldBe("private-bucket");
        capturedRequest.DestinationKey.ShouldBe("backend-template/documents/document.pdf");
        result.ShouldBe("https://account.r2.cloudflarestorage.com/private-bucket/backend-template/documents/document.pdf");
    }
}
