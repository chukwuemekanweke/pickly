namespace BLVK.Pickly.Logistics.Domain.Common.Storage;

public interface IObjectStorageService
{
    Task<string> UploadPublicAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken);
    Task<string> UploadPrivateAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken);
    Task<ObjectStoragePresignedUploadResult> CreatePrivatePresignedUploadAsync(
        ObjectStoragePresignedUploadRequest request,
        CancellationToken cancellationToken);
    Task<ObjectStorageObjectMetadata?> GetPrivateObjectMetadataAsync(
        string objectKey,
        CancellationToken cancellationToken);
    Task<byte[]> ReadPrivateObjectRangeAsync(
        ObjectStorageRangeReadRequest request,
        CancellationToken cancellationToken);
    Task<string> PromotePrivateObjectAsync(
        ObjectStoragePromotionRequest request,
        CancellationToken cancellationToken);
    Task DeletePrivateObjectAsync(string objectKey, CancellationToken cancellationToken);
}
