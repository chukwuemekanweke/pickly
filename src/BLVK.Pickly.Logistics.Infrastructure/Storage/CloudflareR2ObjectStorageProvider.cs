using Amazon.S3;
using Amazon.S3.Model;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using Microsoft.Extensions.Options;
using System.Net;

namespace BLVK.Pickly.Logistics.Infrastructure.Storage;

internal sealed class CloudflareR2ObjectStorageProvider(
    IOptions<CloudflareR2Options> options,
    ICloudflareR2ClientFactory clientFactory) : IObjectStorageProvider
{
    public string ProviderKey => ObjectStorageProviderKeys.CloudflareR2;

    public async Task<string> UploadPublicAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken)
    {
        var configuredOptions = GetConfiguredOptions();
        var objectKey = BuildScopedObjectKey(configuredOptions.ApplicationFolder, request.ObjectKey);
        using var client = clientFactory.Create(configuredOptions);
        await UploadToBucketAsync(client, request, configuredOptions.PublicBucketName.Trim(), objectKey, cancellationToken);
        return BuildPublicUrl(configuredOptions, objectKey);
    }

    public async Task<string> UploadPrivateAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken)
    {
        var configuredOptions = GetConfiguredOptions();
        var objectKey = BuildScopedObjectKey(configuredOptions.ApplicationFolder, request.ObjectKey);
        using var client = clientFactory.Create(configuredOptions);
        await UploadToBucketAsync(client, request, configuredOptions.PrivateBucketName.Trim(), objectKey, cancellationToken);
        return $"{configuredOptions.Endpoint.TrimEnd('/')}/{configuredOptions.PrivateBucketName.Trim()}/{objectKey}";
    }

    public async Task<ObjectStoragePresignedUploadResult> CreatePrivatePresignedUploadAsync(
        ObjectStoragePresignedUploadRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var configuredOptions = GetConfiguredOptions();
        using var client = clientFactory.Create(configuredOptions);
        var uploadUrl = await client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = configuredOptions.PrivateBucketName.Trim(),
            Key = BuildScopedObjectKey(configuredOptions.ApplicationFolder, request.ObjectKey),
            Verb = HttpVerb.PUT,
            ContentType = request.ContentType,
            Expires = request.ExpiresAtUtc.UtcDateTime
        });
        uploadUrl = UseConfiguredEndpointScheme(uploadUrl, configuredOptions.Endpoint);

        return new ObjectStoragePresignedUploadResult(
            uploadUrl,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Content-Type"] = request.ContentType
            },
            request.ExpiresAtUtc);
    }

    public async Task<ObjectStorageObjectMetadata?> GetPrivateObjectMetadataAsync(
        string objectKey,
        CancellationToken cancellationToken)
    {
        var configuredOptions = GetConfiguredOptions();
        using var client = clientFactory.Create(configuredOptions);
        try
        {
            var response = await client.GetObjectMetadataAsync(
                new GetObjectMetadataRequest
                {
                    BucketName = configuredOptions.PrivateBucketName.Trim(),
                    Key = BuildScopedObjectKey(configuredOptions.ApplicationFolder, objectKey)
                },
                cancellationToken);

            return new ObjectStorageObjectMetadata(
                response.Headers.ContentLength,
                response.Headers.ContentType ?? string.Empty,
                response.ETag ?? string.Empty);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<byte[]> ReadPrivateObjectRangeAsync(
        ObjectStorageRangeReadRequest request,
        CancellationToken cancellationToken)
    {
        var configuredOptions = GetConfiguredOptions();
        using var client = clientFactory.Create(configuredOptions);
        try
        {
            using var response = await client.GetObjectAsync(
                new GetObjectRequest
                {
                    BucketName = configuredOptions.PrivateBucketName.Trim(),
                    Key = BuildScopedObjectKey(configuredOptions.ApplicationFolder, request.ObjectKey),
                    ByteRange = new ByteRange(request.Start, request.End),
                    EtagToMatch = request.ExpectedETag
                },
                cancellationToken);
            using var content = new MemoryStream();
            await response.ResponseStream.CopyToAsync(content, cancellationToken);
            return content.ToArray();
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            throw new ObjectStoragePreconditionFailedException("The private object changed before it could be read.", exception);
        }
    }

    public async Task<string> PromotePrivateObjectAsync(
        ObjectStoragePromotionRequest request,
        CancellationToken cancellationToken)
    {
        var configuredOptions = GetConfiguredOptions();
        var destinationObjectKey = BuildScopedObjectKey(configuredOptions.ApplicationFolder, request.DestinationObjectKey);
        var destinationBucketName = request.DestinationVisibility == ObjectStorageVisibility.Public
            ? configuredOptions.PublicBucketName.Trim()
            : configuredOptions.PrivateBucketName.Trim();
        using var client = clientFactory.Create(configuredOptions);
        try
        {
            await client.CopyObjectAsync(new CopyObjectRequest
            {
                SourceBucket = configuredOptions.PrivateBucketName.Trim(),
                SourceKey = BuildScopedObjectKey(configuredOptions.ApplicationFolder, request.SourceObjectKey),
                DestinationBucket = destinationBucketName,
                DestinationKey = destinationObjectKey,
                ETagToMatch = request.ExpectedSourceETag,
                MetadataDirective = S3MetadataDirective.REPLACE,
                ContentType = request.ContentType
            }, cancellationToken);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            throw new ObjectStoragePreconditionFailedException("The private object changed before it could be promoted.", exception);
        }

        return request.DestinationVisibility == ObjectStorageVisibility.Public
            ? BuildPublicUrl(configuredOptions, destinationObjectKey)
            : $"{configuredOptions.Endpoint.TrimEnd('/')}/{destinationBucketName}/{destinationObjectKey}";
    }

    public async Task DeletePrivateObjectAsync(string objectKey, CancellationToken cancellationToken)
    {
        var configuredOptions = GetConfiguredOptions();
        using var client = clientFactory.Create(configuredOptions);
        await client.DeleteObjectAsync(
            configuredOptions.PrivateBucketName.Trim(),
            BuildScopedObjectKey(configuredOptions.ApplicationFolder, objectKey),
            cancellationToken);
    }

    private CloudflareR2Options GetConfiguredOptions()
    {
        var configuredOptions = options.Value;
        EnsureConfigured(configuredOptions);
        return configuredOptions;
    }

    private static async Task UploadToBucketAsync(
        IAmazonS3 client,
        ObjectStorageUploadRequest request,
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken)
    {
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = request.Content,
            ContentType = request.ContentType
        }, cancellationToken);
    }

    private static string BuildPublicUrl(CloudflareR2Options configuredOptions, string objectKey) =>
        $"{configuredOptions.PublicBaseUrl.TrimEnd('/')}/{objectKey}";

    private static string UseConfiguredEndpointScheme(string uploadUrl, string endpoint)
    {
        var configuredEndpoint = new Uri(endpoint, UriKind.Absolute);
        var presignedUrl = new UriBuilder(uploadUrl)
        {
            Scheme = configuredEndpoint.Scheme,
            Port = configuredEndpoint.Port
        };
        return presignedUrl.Uri.AbsoluteUri;
    }

    private static void EnsureConfigured(CloudflareR2Options configuredOptions)
    {
        if (string.IsNullOrWhiteSpace(configuredOptions.Endpoint) ||
            string.IsNullOrWhiteSpace(configuredOptions.ApplicationFolder) ||
            string.IsNullOrWhiteSpace(configuredOptions.PublicBucketName) ||
            string.IsNullOrWhiteSpace(configuredOptions.PrivateBucketName) ||
            string.IsNullOrWhiteSpace(configuredOptions.AccessKeyId) ||
            string.IsNullOrWhiteSpace(configuredOptions.SecretAccessKey) ||
            string.IsNullOrWhiteSpace(configuredOptions.PublicBaseUrl))
        {
            throw new InvalidOperationException(
                "Cloudflare R2 configuration is incomplete. Ensure Endpoint, ApplicationFolder, PublicBucketName, PrivateBucketName, AccessKeyId, SecretAccessKey, and PublicBaseUrl are provided.");
        }
    }

    internal static string BuildScopedObjectKey(string applicationFolder, string objectKey)
    {
        var normalizedFolder = NormalizeObjectKey(applicationFolder).TrimEnd('/');
        var normalizedObjectKey = NormalizeObjectKey(objectKey);
        return normalizedObjectKey.StartsWith($"{normalizedFolder}/", StringComparison.Ordinal)
            ? normalizedObjectKey
            : $"{normalizedFolder}/{normalizedObjectKey}";
    }

    private static string NormalizeObjectKey(string objectKey) =>
        objectKey.TrimStart('/').Replace("\\", "/", StringComparison.Ordinal);
}
