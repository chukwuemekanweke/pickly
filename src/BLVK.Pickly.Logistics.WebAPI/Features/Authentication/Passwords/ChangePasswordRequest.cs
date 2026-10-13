namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Passwords;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);
