namespace BLVK.Pickly.Logistics.Domain.Common.Authentication;

public sealed record RegistrationPolicy(
    bool IsMvp,
    bool RequireProfileDetails,
    int EmailConfirmationOtpLength)
{
    public static RegistrationPolicy Create(bool isMvp) =>
        new(isMvp, RequireProfileDetails: !isMvp, EmailConfirmationOtpLength: isMvp ? 5 : 6);
}
