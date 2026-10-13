namespace BLVK.Pickly.Logistics.Application.Stakeholders.Features.CompleteAvatarUpload;

public sealed record CompleteAvatarUploadResult(
    CompleteAvatarUploadStatus Status,
    string? AvatarUrl = null,
    string? Error = null);
