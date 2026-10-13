using BLVK.Pickly.Logistics.Domain.Common.Storage;

namespace BLVK.Pickly.Logistics.Application.Common.FileUploads;

public sealed record FileUploadCompletionRequest(
    string QuarantineObjectKey,
    string FinalObjectKey,
    string ExpectedContentType,
    long ExpectedContentLength,
    ObjectStorageVisibility DestinationVisibility);
