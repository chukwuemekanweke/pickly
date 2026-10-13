using BLVK.Pickly.Logistics.Domain.Common.Storage;

namespace BLVK.Pickly.Logistics.Application.Common.FileUploads;

public sealed class FileUploadService(IObjectStorageService objectStorageService)
{
    private const int MaxFileNameLength = 255;

    public FileUploadPreparationResult Prepare(
        FileUploadPreparationRequest request,
        IFileUploadPolicy policy,
        DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(request.OwnerType))
        {
            throw new ArgumentException("An owner type is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.FileName) ||
            request.FileName.Length > MaxFileNameLength ||
            string.IsNullOrWhiteSpace(request.ContentType) ||
            request.ContentLength <= 0 ||
            request.ContentLength > policy.MaxFileSizeBytes)
        {
            return new FileUploadPreparationResult(false, Error: policy.InvalidFileError);
        }

        var contentType = request.ContentType.Trim().ToLowerInvariant();
        if (!policy.TryGetFileExtension(contentType, out var extension))
        {
            return new FileUploadPreparationResult(false, Error: policy.InvalidFileError);
        }

        var uploadId = Guid.CreateVersion7();
        var pathContext = new FileUploadPathContext(
            uploadId,
            request.TenantId,
            request.OwnerType.Trim(),
            request.OwnerId,
            extension);
        return new FileUploadPreparationResult(
            true,
            uploadId,
            request.FileName.Trim(),
            contentType,
            request.ContentLength,
            extension,
            policy.BuildQuarantineObjectKey(pathContext),
            policy.BuildFinalObjectKey(pathContext),
            utcNow.Add(policy.UploadLifetime));
    }

    public Task<ObjectStoragePresignedUploadResult> CreatePresignedUploadAsync(
        FileUploadPreparationResult preparation,
        CancellationToken cancellationToken)
    {
        EnsureValidPreparation(preparation);
        return objectStorageService.CreatePrivatePresignedUploadAsync(
            new ObjectStoragePresignedUploadRequest(
                preparation.QuarantineObjectKey!,
                preparation.ContentType!,
                preparation.ExpiresAtUtc!.Value),
            cancellationToken);
    }

    public async Task<FileUploadCompletionResult> CompleteAsync(
        FileUploadCompletionRequest request,
        IFileUploadPolicy policy,
        CancellationToken cancellationToken)
    {
        var metadata = await objectStorageService.GetPrivateObjectMetadataAsync(
            request.QuarantineObjectKey,
            cancellationToken);
        if (metadata is null ||
            metadata.ContentLength <= 0 ||
            metadata.ContentLength > policy.MaxFileSizeBytes ||
            metadata.ContentLength != request.ExpectedContentLength ||
            !string.Equals(metadata.ContentType.Trim(), request.ExpectedContentType, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(metadata.ETag))
        {
            return new FileUploadCompletionResult(
                FileUploadCompletionStatus.InvalidMetadata,
                FailureReason: "invalid_metadata");
        }

        byte[] signature;
        try
        {
            signature = await objectStorageService.ReadPrivateObjectRangeAsync(
                new ObjectStorageRangeReadRequest(
                    request.QuarantineObjectKey,
                    0,
                    policy.SignatureByteCount - 1,
                    metadata.ETag),
                cancellationToken);
        }
        catch (ObjectStoragePreconditionFailedException)
        {
            return ObjectChanged();
        }

        if (!policy.MatchesSignature(request.ExpectedContentType, signature))
        {
            return new FileUploadCompletionResult(
                FileUploadCompletionStatus.InvalidSignature,
                FailureReason: "invalid_signature");
        }

        try
        {
            var finalUrl = await objectStorageService.PromotePrivateObjectAsync(
                new ObjectStoragePromotionRequest(
                    request.QuarantineObjectKey,
                    request.FinalObjectKey,
                    metadata.ETag,
                    request.ExpectedContentType,
                    request.DestinationVisibility),
                cancellationToken);
            return new FileUploadCompletionResult(
                FileUploadCompletionStatus.Success,
                finalUrl,
                metadata.ETag);
        }
        catch (ObjectStoragePreconditionFailedException)
        {
            return ObjectChanged();
        }
    }

    private static FileUploadCompletionResult ObjectChanged() =>
        new(FileUploadCompletionStatus.ObjectChanged, FailureReason: "object_changed");

    private static void EnsureValidPreparation(FileUploadPreparationResult preparation)
    {
        if (!preparation.IsValid ||
            string.IsNullOrWhiteSpace(preparation.QuarantineObjectKey) ||
            string.IsNullOrWhiteSpace(preparation.ContentType) ||
            !preparation.ExpiresAtUtc.HasValue)
        {
            throw new ArgumentException("A valid file upload preparation is required.", nameof(preparation));
        }
    }
}
