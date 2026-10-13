namespace BLVK.Pickly.Logistics.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenValidateVerificationRequest(
    string IdentityId,
    string Type,
    string Otp);
