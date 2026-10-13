using Amazon.Runtime;
using Amazon.S3;

namespace BLVK.Pickly.Logistics.Infrastructure.Storage;

internal sealed class CloudflareR2ClientFactory : ICloudflareR2ClientFactory
{
    public IAmazonS3 Create(CloudflareR2Options options)
    {
        var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
        var endpoint = new Uri(options.Endpoint.TrimEnd('/'), UriKind.Absolute);
        return new AmazonS3Client(credentials, new AmazonS3Config
        {
            ServiceURL = endpoint.ToString().TrimEnd('/'),
            ForcePathStyle = true,
            AuthenticationRegion = "auto",
            UseHttp = endpoint.Scheme == Uri.UriSchemeHttp
        });
    }
}
