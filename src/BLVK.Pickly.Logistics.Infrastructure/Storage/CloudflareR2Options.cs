namespace BLVK.Pickly.Logistics.Infrastructure.Storage;

public sealed class CloudflareR2Options
{
    public const string SectionName = "ObjectStorage:CloudflareR2";

    public string Endpoint { get; set; } = string.Empty;
    public string ApplicationFolder { get; set; } = string.Empty;
    public string PublicBucketName { get; set; } = string.Empty;
    public string PrivateBucketName { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;
}
