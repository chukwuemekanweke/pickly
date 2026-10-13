namespace BLVK.Pickly.Logistics.Domain.Common.Storage;

public sealed record ObjectStoragePresignedUploadRequest(
    string ObjectKey,
    string ContentType,
    DateTimeOffset ExpiresAtUtc);
