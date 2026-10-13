namespace BLVK.Pickly.Logistics.Infrastructure.Payments.Credo;

public sealed class CredoOptions
{
    public const string SectionName = "Payments:Credo";

    public string BaseUrl { get; set; } = "https://api.credodemo.com";
    public string PublicKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string? CallbackUrl { get; set; }
    public int Bearer { get; set; }
    public int InitializeAccount { get; set; }
    public string[] Channels { get; set; } = ["CARD", "BANK"];
}
