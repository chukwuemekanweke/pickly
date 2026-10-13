namespace BLVK.Pickly.Logistics.Application.Common.FileUploads;

public enum FileUploadCompletionStatus
{
    Success = 1,
    InvalidMetadata = 2,
    InvalidSignature = 3,
    ObjectChanged = 4
}
