namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.PasswordResets;

public sealed record CompletePasswordResetRequest(
    string Email,
    string Otp,
    string Password,
    string ConfirmPassword);
