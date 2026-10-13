namespace BLVK.Pickly.Logistics.Domain.Common.Storage;

public sealed record ObjectStorageRangeReadRequest(
    string ObjectKey,
    long Start,
    long End,
    string ExpectedETag);
