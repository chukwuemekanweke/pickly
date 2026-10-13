namespace BLVK.Pickly.Logistics.Application.Stakeholders.Features.CreateAvatarUpload;

public sealed record CreateAvatarUploadResult(
    CreateAvatarUploadStatus Status,
    Guid? UploadId = null,
    string? UploadUrl = null,
    IReadOnlyDictionary<string, string>? Headers = null,
    DateTimeOffset? ExpiresAtUtc = null,
    string? Error = null);
