using BLVK.Pickly.Logistics.Application.Common.FileUploads;
using BLVK.Pickly.Logistics.Domain.Common.Storage;

namespace BLVK.Pickly.Logistics.Application.Stakeholders.AvatarUploads;

internal sealed class AvatarUploadPolicy : IFileUploadPolicy
{
    private static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

    public static AvatarUploadPolicy Instance { get; } = new();

    public string Key => "stakeholder-avatar-v1";
    public long MaxFileSizeBytes => 2 * 1024 * 1024;
    public int SignatureByteCount => 12;
    public TimeSpan UploadLifetime => TimeSpan.FromMinutes(10);
    public ObjectStorageVisibility DestinationVisibility => ObjectStorageVisibility.Public;
    public string InvalidFileError => "Avatar must be a JPEG, PNG, or WEBP file with size up to 2 MB.";

    public bool TryGetFileExtension(string contentType, out string extension) =>
        AllowedContentTypes.TryGetValue(contentType, out extension!);

    public bool MatchesSignature(string contentType, ReadOnlySpan<byte> bytes) => contentType switch
    {
        "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
        "image/png" => bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        "image/webp" => bytes.Length >= 12 &&
            bytes[..4].SequenceEqual("RIFF"u8) &&
            bytes.Slice(8, 4).SequenceEqual("WEBP"u8),
        _ => false
    };

    public string BuildQuarantineObjectKey(FileUploadPathContext context) =>
        $"quarantine/avatars/tenants/{context.TenantId}/stakeholders/{context.OwnerId}/{context.UploadId:N}{context.FileExtension}";

    public string BuildFinalObjectKey(FileUploadPathContext context) =>
        $"tenants/{context.TenantId}/stakeholders/{context.OwnerId}/avatar/{context.UploadId:N}{context.FileExtension}";
}
