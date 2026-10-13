namespace BLVK.Pickly.Logistics.Infrastructure.Storage;

public sealed class ObjectStorageOptions
{
    public const string SectionName = "ObjectStorage";

    public int AvatarMaxFileSizeBytes { get; set; } = 2 * 1024 * 1024;
}
