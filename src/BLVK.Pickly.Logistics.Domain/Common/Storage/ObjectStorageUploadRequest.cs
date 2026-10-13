namespace BLVK.Pickly.Logistics.Domain.Common.Storage;

public sealed record ObjectStorageUploadRequest(
    string ObjectKey,
    Stream Content,
    string ContentType);
