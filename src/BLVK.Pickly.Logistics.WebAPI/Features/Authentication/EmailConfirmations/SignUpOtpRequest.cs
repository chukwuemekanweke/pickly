namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.EmailConfirmations;

public sealed record SignUpOtpRequest(string Email, string Otp);
