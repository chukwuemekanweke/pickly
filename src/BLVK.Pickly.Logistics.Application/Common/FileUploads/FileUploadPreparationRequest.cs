namespace BLVK.Pickly.Logistics.Application.Common.FileUploads;

public sealed record FileUploadPreparationRequest(
    Guid TenantId,
    string OwnerType,
    Guid OwnerId,
    string FileName,
    string ContentType,
    long ContentLength);
