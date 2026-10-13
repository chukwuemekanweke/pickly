namespace BLVK.Pickly.Logistics.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenCreateSubAccountRequest(
    string PhoneNumber,
    string Email,
    string ExternalReference,
    string IdentityType,
    string? IdentityNumber,
    string? IdentityId,
    string? Otp);
