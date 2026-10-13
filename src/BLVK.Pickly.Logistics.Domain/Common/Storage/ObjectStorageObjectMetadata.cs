namespace BLVK.Pickly.Logistics.Domain.Common.Storage;

public sealed record ObjectStorageObjectMetadata(
    long ContentLength,
    string ContentType,
    string ETag);
