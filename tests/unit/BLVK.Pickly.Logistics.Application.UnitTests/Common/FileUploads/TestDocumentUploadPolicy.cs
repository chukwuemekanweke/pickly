using BLVK.Pickly.Logistics.Application.Common.FileUploads;
using BLVK.Pickly.Logistics.Domain.Common.Storage;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Common.FileUploads;

internal sealed class TestDocumentUploadPolicy : IFileUploadPolicy
{
    public string Key => "test-pdf-document-v1";
    public long MaxFileSizeBytes => 10 * 1024 * 1024;
    public int SignatureByteCount => 5;
    public TimeSpan UploadLifetime => TimeSpan.FromMinutes(20);
    public ObjectStorageVisibility DestinationVisibility => ObjectStorageVisibility.Private;
    public string InvalidFileError => "Document must be a PDF file with size up to 10 MB.";

    public bool TryGetFileExtension(string contentType, out string extension)
    {
        extension = ".pdf";
        return string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
    }

    public bool MatchesSignature(string contentType, ReadOnlySpan<byte> bytes) =>
        string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase) &&
        bytes.StartsWith("%PDF-"u8);

    public string BuildQuarantineObjectKey(FileUploadPathContext context) =>
        $"quarantine/documents/tenants/{context.TenantId}/owners/{context.OwnerId}/{context.UploadId:N}{context.FileExtension}";

    public string BuildFinalObjectKey(FileUploadPathContext context) =>
        $"tenants/{context.TenantId}/owners/{context.OwnerId}/documents/{context.UploadId:N}{context.FileExtension}";
}
