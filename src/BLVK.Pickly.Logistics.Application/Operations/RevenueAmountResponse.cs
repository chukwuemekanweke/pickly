namespace BLVK.Pickly.Logistics.Application.Operations;

/// <param name="Amount">Charges of deliveries completed in the period, in <paramref name="CurrencyCode"/>.</param>
public sealed record RevenueAmountResponse(string CurrencyCode, decimal Amount);
