using BLVK.Pickly.Logistics.Domain.Common.Storage;

namespace BLVK.Pickly.Logistics.Application.Common.FileUploads;

public interface IFileUploadPolicy
{
    string Key { get; }
    long MaxFileSizeBytes { get; }
    int SignatureByteCount { get; }
    TimeSpan UploadLifetime { get; }
    ObjectStorageVisibility DestinationVisibility { get; }
    string InvalidFileError { get; }

    bool TryGetFileExtension(string contentType, out string extension);
    bool MatchesSignature(string contentType, ReadOnlySpan<byte> bytes);
    string BuildQuarantineObjectKey(FileUploadPathContext context);
    string BuildFinalObjectKey(FileUploadPathContext context);
}
