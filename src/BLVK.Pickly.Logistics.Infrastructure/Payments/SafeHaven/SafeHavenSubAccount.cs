using System.Text.Json.Serialization;

namespace BLVK.Pickly.Logistics.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenSubAccount
{
    [JsonPropertyName("_id")]
    public string Id { get; init; } = string.Empty;

    public string Client { get; init; } = string.Empty;

    public string AccountProduct { get; init; } = string.Empty;

    public string AccountNumber { get; init; } = string.Empty;

    public string AccountName { get; init; } = string.Empty;

    public string AccountType { get; init; } = string.Empty;

    public string CurrencyCode { get; init; } = string.Empty;

    public string Bvn { get; init; } = string.Empty;

    public string IdentityId { get; init; } = string.Empty;

    public decimal AccountBalance { get; init; }

    public decimal BookBalance { get; init; }

    public string CallbackUrl { get; init; } = string.Empty;

    public bool IsSubAccount { get; init; }

    public SubAccountDetailsDto SubAccountDetails { get; init; } = new();

    public string ExternalReference { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public string Nin { get; init; } = string.Empty;

    public string CbaAccountId { get; init; } = string.Empty;

    public sealed record SubAccountDetailsDto
    {
        [JsonPropertyName("_id")]
        public string Id { get; init; } = string.Empty;

        public string FirstName { get; init; } = string.Empty;

        public string LastName { get; init; } = string.Empty;

        public string EmailAddress { get; init; } = string.Empty;

        public string Bvn { get; init; } = string.Empty;
    }
}
