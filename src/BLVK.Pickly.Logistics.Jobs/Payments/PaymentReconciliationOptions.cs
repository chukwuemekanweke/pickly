namespace BLVK.Pickly.Logistics.Jobs.Payments;

public sealed class PaymentReconciliationOptions
{
    public const string SectionName = "Jobs:Payments:Reconciliation";

    public int BatchSize { get; set; } = 50;
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan StaleThreshold { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan RecheckDelay { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan MaxInitiatedAge { get; set; } = TimeSpan.FromHours(48);
}
