namespace BLVK.Pickly.Logistics.Application.Common.FileUploads;

public sealed record FileUploadPreparationResult(
    bool IsValid,
    Guid? UploadId = null,
    string? OriginalFileName = null,
    string? ContentType = null,
    long? ContentLength = null,
    string? FileExtension = null,
    string? QuarantineObjectKey = null,
    string? FinalObjectKey = null,
    DateTimeOffset? ExpiresAtUtc = null,
    string? Error = null);
