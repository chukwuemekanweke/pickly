namespace BLVK.Pickly.Logistics.WebAPI;

public static class EndpointUrl
{
    public static class Versions
    {
        public const string V1 = "v1";
        public const string V1Route = "v{version:apiVersion}";
    }

    public static class Registrations
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/registrations";
        public static readonly string V1 = ToV1(Route);
    }

    public static class EmailConfirmations
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/email-confirmations";
        public static readonly string V1 = ToV1(Route);
        public static readonly string ConfirmationCodeV1 = $"{V1}/confirmation-code";
    }

    public static class EmailExistenceChecks
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/email-existence-checks";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Sessions
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/sessions";
        public static readonly string V1 = ToV1(Route);
        public static readonly string GoogleV1 = $"{V1}/google";
        public static readonly string RefreshV1 = $"{V1}/refresh";
        public static readonly string LogoutV1 = $"{V1}/logout";
        public static readonly string TwoFactorV1 = $"{V1}/two-factor";
    }

    public static class AuthenticationSecurity
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/security";
        public static readonly string V1 = ToV1(Route);
        public static readonly string TwoFactorV1 = $"{V1}/two-factor";
    }

    public static class GoogleRegistrations
    {
        public const string Route = $"{Registrations.Route}/google";
        public static readonly string V1 = ToV1(Route);
    }

    public static class GoogleAuthenticationFlows
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/google/flows";
        public static readonly string V1 = ToV1(Route);
    }

    public static class GoogleLinks
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/google-links";
        public static readonly string V1 = ToV1(Route);
    }

    public static class PasswordResets
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/password-resets";
        public static readonly string V1 = ToV1(Route);
        public static readonly string CompletionsV1 = $"{V1}/completions";
    }

    public static class Passwords
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/password";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Countries
    {
        public const string Route = $"api/{Versions.V1Route}/reference-data/countries";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Languages
    {
        public const string Route = $"api/{Versions.V1Route}/reference-data/languages";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Stakeholders
    {
        public const string Route = $"api/{Versions.V1Route}/stakeholders";
        public static readonly string V1 = ToV1(Route);
        public static readonly string LoginActivityV1 = $"{V1}/me/login-activity";
    }

    public static class Providers
    {
        public const string Route = $"api/{Versions.V1Route}/providers";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Payments
    {
        public const string Route = $"api/{Versions.V1Route}/payments";
        public static readonly string V1 = ToV1(Route);
        public static readonly string InitiateV1 = $"{V1}/initiate";
        public static readonly string WalletTransactionsV1 = $"{V1}/wallet-transactions";
        public static string WalletTopUpTransactionDetailsV1(Guid walletTransactionId) =>
            $"{WalletTransactionsV1}/top-ups/{walletTransactionId}";
    }

    public static class PaymentProviders
    {
        public const string Route = $"api/{Versions.V1Route}/payment-providers";
        public static readonly string V1 = ToV1(Route);
    }

    public static class PickupLocations
    {
        public const string Route = $"api/{Versions.V1Route}/pickup-locations";
        public static readonly string V1 = ToV1(Route);
        public static readonly string DefaultV1 = $"{V1}/default";
        public static string ByIdV1(Guid locationId) => $"{V1}/{locationId}";
    }

    public static class Locations
    {
        public const string Route = $"api/{Versions.V1Route}/locations";
        public static readonly string V1 = ToV1(Route);
        public static readonly string SearchV1 = $"{V1}/search";
        public static readonly string ResolveV1 = $"{V1}/resolve";
        public static readonly string ResolveSelectionsV1 = $"{V1}/resolve/selections";
        public static readonly string NearbyV1 = $"{V1}/nearby";
        public static string ByIdV1(Guid locationId) => $"{V1}/{locationId}";
    }

    public static class PaymentWebhooks
    {
        public static class SafeHaven
        {
            public const string Route = $"api/{Versions.V1Route}/payments/webhooks/safehaven";
            public static readonly string V1 = ToV1(Route);
        }

        public static class Credo
        {
            public const string Route = $"api/{Versions.V1Route}/payments/webhooks/credo";
            public static readonly string V1 = ToV1(Route);
        }
    }

    public static class EmailNotificationWebhooks
    {
        public static class Mailtrap
        {
            public const string Route = $"api/{Versions.V1Route}/email-notifications/webhooks/mailtrap";
            public static readonly string V1 = ToV1(Route);
        }
    }

    private static string ToV1(string routeTemplate) =>
        routeTemplate.Replace(Versions.V1Route, Versions.V1, StringComparison.Ordinal);
}
