namespace BLVK.Pickly.Logistics.Application.Stakeholders.Features.CompleteAvatarUpload;

public enum CompleteAvatarUploadStatus
{
    Success = 1,
    NotAuthenticated = 2,
    StakeholderNotFound = 3,
    UploadNotFound = 4,
    Expired = 5,
    InvalidFile = 6,
    UploadChanged = 7
}
