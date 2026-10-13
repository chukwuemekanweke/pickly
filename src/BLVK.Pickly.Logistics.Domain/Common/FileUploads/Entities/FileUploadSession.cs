using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Common.Storage;

namespace BLVK.Pickly.Logistics.Domain.Common.FileUploads.Entities;

public sealed class FileUploadSession : Entity, IAggregateRoot
{
    private FileUploadSession()
    {
    }

    private FileUploadSession(
        Guid id,
        Guid tenantId,
        string ownerType,
        Guid ownerId,
        Guid? initiatedByStakeholderId,
        string purpose,
        string policyKey,
        string originalFileName,
        string expectedContentType,
        long expectedContentLength,
        string fileExtension,
        string quarantineObjectKey,
        string finalObjectKey,
        ObjectStorageVisibility destinationVisibility,
        DateTimeOffset expiresAtUtc)
    {
        Id = id;
        TenantId = tenantId;
        OwnerType = RequireValue(ownerType, nameof(ownerType));
        OwnerId = ownerId;
        InitiatedByStakeholderId = initiatedByStakeholderId;
        Purpose = RequireValue(purpose, nameof(purpose));
        PolicyKey = RequireValue(policyKey, nameof(policyKey));
        OriginalFileName = RequireValue(originalFileName, nameof(originalFileName));
        ExpectedContentType = RequireValue(expectedContentType, nameof(expectedContentType));
        ExpectedContentLength = expectedContentLength;
        FileExtension = RequireValue(fileExtension, nameof(fileExtension));
        QuarantineObjectKey = RequireValue(quarantineObjectKey, nameof(quarantineObjectKey));
        FinalObjectKey = RequireValue(finalObjectKey, nameof(finalObjectKey));
        DestinationVisibility = destinationVisibility;
        ExpiresAtUtc = expiresAtUtc;
        Status = FileUploadStatus.Pending;
    }

    public Guid TenantId { get; private set; }
    public string OwnerType { get; private set; } = string.Empty;
    public Guid OwnerId { get; private set; }
    public Guid? InitiatedByStakeholderId { get; private set; }
    public string Purpose { get; private set; } = string.Empty;
    public string PolicyKey { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ExpectedContentType { get; private set; } = string.Empty;
    public long ExpectedContentLength { get; private set; }
    public string FileExtension { get; private set; } = string.Empty;
    public string QuarantineObjectKey { get; private set; } = string.Empty;
    public string FinalObjectKey { get; private set; } = string.Empty;
    public ObjectStorageVisibility DestinationVisibility { get; private set; }
    public string? FinalLocation { get; private set; }
    public string? ValidatedETag { get; private set; }
    public FileUploadStatus Status { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }

    public static FileUploadSession Create(
        Guid id,
        Guid tenantId,
        string ownerType,
        Guid ownerId,
        Guid? initiatedByStakeholderId,
        string purpose,
        string policyKey,
        string originalFileName,
        string expectedContentType,
        long expectedContentLength,
        string fileExtension,
        string quarantineObjectKey,
        string finalObjectKey,
        ObjectStorageVisibility destinationVisibility,
        DateTimeOffset expiresAtUtc) =>
        new(
            id,
            tenantId,
            ownerType,
            ownerId,
            initiatedByStakeholderId,
            purpose,
            policyKey,
            originalFileName,
            expectedContentType,
            expectedContentLength,
            fileExtension,
            quarantineObjectKey,
            finalObjectKey,
            destinationVisibility,
            expiresAtUtc);

    public void MarkCompleted(string finalLocation, string validatedETag)
    {
        EnsurePending();
        FinalLocation = RequireValue(finalLocation, nameof(finalLocation));
        ValidatedETag = RequireValue(validatedETag, nameof(validatedETag));
        RejectionReason = null;
        Status = FileUploadStatus.Completed;
    }

    public void Reject(string reason)
    {
        EnsurePending();
        RejectionReason = RequireValue(reason, nameof(reason));
        Status = FileUploadStatus.Rejected;
    }

    public void MarkExpired()
    {
        EnsurePending();
        Status = FileUploadStatus.Expired;
    }

    private void EnsurePending()
    {
        if (Status != FileUploadStatus.Pending)
        {
            throw new AggregateStateException($"File upload session in '{Status}' state cannot be changed.");
        }
    }

    private static string RequireValue(string value, string argumentName)
    {
        var normalized = value.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? throw new ArgumentException("A value is required.", argumentName)
            : normalized;
    }
}
