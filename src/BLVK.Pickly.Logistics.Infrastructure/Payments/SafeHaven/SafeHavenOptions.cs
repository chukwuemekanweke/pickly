namespace BLVK.Pickly.Logistics.Infrastructure.Payments.SafeHaven;

public sealed class SafeHavenOptions
{
    public const string SectionName = "Payments:SafeHaven";

    public string BaseUrl { get; set; } = "https://api.sandbox.safehavenmfb.com";
    public string ClientId { get; set; } = string.Empty;
    public string ClientAssertion { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public string AutoSweepAccountNumber { get; set; } = string.Empty;
    public int ValidFor { get; set; } = 900;
    public string SettlementBankCode { get; set; } = string.Empty;
    public string SettlementAccountNumber { get; set; } = string.Empty;
}
