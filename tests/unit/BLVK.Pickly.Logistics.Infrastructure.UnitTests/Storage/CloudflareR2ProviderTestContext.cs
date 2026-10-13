using Amazon.S3;
using BLVK.Pickly.Logistics.Infrastructure.Storage;
using NSubstitute;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Storage;

internal sealed class CloudflareR2ProviderTestContext
{
    public IAmazonS3 Client { get; } = Substitute.For<IAmazonS3>();
    public CloudflareR2Options Options { get; } = new()
    {
        Endpoint = "https://account.r2.cloudflarestorage.com",
        ApplicationFolder = "backend-template",
        PublicBucketName = "public-bucket",
        PrivateBucketName = "private-bucket",
        AccessKeyId = "access-key",
        SecretAccessKey = "secret-key",
        PublicBaseUrl = "https://cdn.example.com"
    };

    public CloudflareR2ObjectStorageProvider CreateProvider()
    {
        var clientFactory = Substitute.For<ICloudflareR2ClientFactory>();
        clientFactory.Create(Arg.Any<CloudflareR2Options>()).Returns(Client);
        return new CloudflareR2ObjectStorageProvider(Microsoft.Extensions.Options.Options.Create(Options), clientFactory);
    }
}
