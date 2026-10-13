namespace BLVK.Pickly.Logistics.Application.Common.FileUploads;

public sealed record FileUploadPathContext(
    Guid UploadId,
    Guid TenantId,
    string OwnerType,
    Guid OwnerId,
    string FileExtension);
