namespace BLVK.Pickly.Logistics.Infrastructure.Payments.SafeHaven.Payloads;

internal sealed record SafeHavenCreateSubAccountPayload(
    string PhoneNumber,
    string Email,
    string ExternalReference,
    string IdentityType,
    string? IdentityNumber,
    string? IdentityId,
    string? Otp,
    string CallbackUrl,
    bool AutoSweep,
    SafeHavenCreateSubAccountAutoSweepDetailsPayload AutoSweepDetails);
