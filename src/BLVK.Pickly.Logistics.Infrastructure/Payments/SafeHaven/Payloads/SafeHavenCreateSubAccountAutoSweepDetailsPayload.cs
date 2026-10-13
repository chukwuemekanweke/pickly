namespace BLVK.Pickly.Logistics.Infrastructure.Payments.SafeHaven.Payloads;

internal sealed record SafeHavenCreateSubAccountAutoSweepDetailsPayload(
    string AccountNumber,
    string Schedule);
