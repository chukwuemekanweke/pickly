namespace BLVK.Pickly.Logistics.Domain.Common.Authentication;

public static class GoogleEmailAuthority
{
    public static bool IsAuthoritative(GoogleIdentityTokenPayload identity)
    {
        if (!identity.EmailVerified)
        {
            return false;
        }

        var domainSeparator = identity.Email.LastIndexOf('@');
        var emailDomain = domainSeparator >= 0 ? identity.Email[(domainSeparator + 1)..] : string.Empty;

        return emailDomain.Equals("gmail.com", StringComparison.OrdinalIgnoreCase)
            || emailDomain.Equals("googlemail.com", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(identity.HostedDomain);
    }
}
