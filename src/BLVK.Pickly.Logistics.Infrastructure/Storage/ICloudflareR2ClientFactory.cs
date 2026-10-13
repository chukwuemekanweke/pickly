using Amazon.S3;

namespace BLVK.Pickly.Logistics.Infrastructure.Storage;

internal interface ICloudflareR2ClientFactory
{
    IAmazonS3 Create(CloudflareR2Options options);
}
