using System.Text.Json.Serialization;

namespace BLVK.Pickly.Logistics.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenAccountStatementEntry
{
    public bool IsReversal { get; init; }

    [JsonPropertyName("_id")]
    public string Id { get; init; } = string.Empty;

    public string? CbaTransactionId { get; init; }

    public string Client { get; init; } = string.Empty;

    public AccountDto Account { get; init; } = new();

    public string PaymentReference { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string Provider { get; init; } = string.Empty;

    public string ProviderChannel { get; init; } = string.Empty;

    public string PaymentServices { get; init; } = string.Empty;

    public string Narration { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public decimal RunningBalance { get; init; }

    public DateTimeOffset TransactionDate { get; init; }

    public DateTimeOffset ValueDate { get; init; }

    [JsonPropertyName("__v")]
    public int Version { get; init; }

    public sealed record AccountDto
    {
        public bool CanDebit { get; init; }

        public bool CanCredit { get; init; }

        [JsonPropertyName("_id")]
        public string Id { get; init; } = string.Empty;

        public string Client { get; init; } = string.Empty;

        public string AccountProduct { get; init; } = string.Empty;

        public string AccountNumber { get; init; } = string.Empty;

        public string AccountName { get; init; } = string.Empty;

        public string AccountType { get; init; } = string.Empty;

        public string CurrencyCode { get; init; } = string.Empty;

        public string Bvn { get; init; } = string.Empty;

        public decimal AccountBalance { get; init; }

        public decimal BookBalance { get; init; }

        public decimal InterestBalance { get; init; }

        public decimal WithHoldingTaxBalance { get; init; }

        public string Status { get; init; } = string.Empty;

        public bool IsDefault { get; init; }

        public decimal NominalAnnualInterestRate { get; init; }

        public string InterestCompoundingPeriod { get; init; } = string.Empty;

        public string InterestPostingPeriod { get; init; } = string.Empty;

        public string InterestCalculationType { get; init; } = string.Empty;

        public string InterestCalculationDaysInYearType { get; init; } = string.Empty;

        public decimal MinRequiredOpeningBalance { get; init; }

        public int LockinPeriodFrequency { get; init; }

        public string LockinPeriodFrequencyType { get; init; } = string.Empty;

        public bool AllowOverdraft { get; init; }

        public decimal OverdraftLimit { get; init; }

        public bool ChargeWithHoldingTax { get; init; }

        public bool ChargeValueAddedTax { get; init; }

        public bool ChargeStampDuty { get; init; }

        public NotificationSettingsDto NotificationSettings { get; init; } = new();

        public bool IsSubAccount { get; init; }

        public bool IsDeleted { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset UpdatedAt { get; init; }

        [JsonPropertyName("__v")]
        public int Version { get; init; }

        public string CbaAccountId { get; init; } = string.Empty;

        public sealed record NotificationSettingsDto
        {
            public bool SmsNotification { get; init; }

            public bool EmailNotification { get; init; }

            public bool EmailMonthlyStatement { get; init; }

            public bool SmsMonthlyStatement { get; init; }
        }
    }
}
