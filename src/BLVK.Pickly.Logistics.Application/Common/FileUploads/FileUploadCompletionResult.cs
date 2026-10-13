namespace BLVK.Pickly.Logistics.Application.Common.FileUploads;

public sealed record FileUploadCompletionResult(
    FileUploadCompletionStatus Status,
    string? FinalUrl = null,
    string? ValidatedETag = null,
    string? FailureReason = null);
