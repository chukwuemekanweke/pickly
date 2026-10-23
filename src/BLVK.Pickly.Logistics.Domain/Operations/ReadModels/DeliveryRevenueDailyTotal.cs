namespace BLVK.Pickly.Logistics.Domain.Operations.ReadModels;

/// <summary>
/// Sum of the charges of distinct deliveries that entered <c>Delivered</c> on <see cref="Day"/>, in one currency.
/// </summary>
public sealed record DeliveryRevenueDailyTotal(
    DateOnly Day,
    string CurrencyCode,
    decimal Amount);
