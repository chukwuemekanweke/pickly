using System.Text.Json.Serialization;

namespace BLVK.Pickly.Logistics.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenVirtualAccount
{
    [JsonPropertyName("_id")]
    public string Id { get; init; } = string.Empty;

    public string Client { get; init; } = string.Empty;

    public string BankCode { get; init; } = string.Empty;

    public string AccountNumber { get; init; } = string.Empty;

    public string AccountName { get; init; } = string.Empty;

    public string CurrencyCode { get; init; } = string.Empty;

    public string Bvn { get; init; } = string.Empty;

    public int ValidFor { get; init; }

    public string AmountControl { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public DateTimeOffset ExpiryDate { get; init; }

    public string CallbackUrl { get; init; } = string.Empty;

    public SettlementAccountDto SettlementAccount { get; init; } = new();

    public string Status { get; init; } = string.Empty;

    public bool IsDeleted { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public sealed record SettlementAccountDto
    {
        public string BankCode { get; init; } = string.Empty;

        public string AccountNumber { get; init; } = string.Empty;
    }
}
