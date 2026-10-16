using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Quotes;

public sealed record DeliveryQuoteResponse(Guid QuoteId, string DeliveryType, string OrderType, DateTimeOffset? ScheduledPickupAtUtc, QuoteVehicleResponse VehicleCategory, long DistanceMeters, string OriginZone, string DestinationZone, QuotePricingResponse Pricing, string PaymentResponsibleParty, string PaymentMethod, DateTimeOffset IssuedAtUtc, DateTimeOffset ExpiresAtUtc, string Status, QuoteAddressResponse Pickup, QuoteAddressResponse DropOff, IReadOnlyList<QuoteItemResponse> Items)
{
    public static DeliveryQuoteResponse From(DeliveryQuote quote, string currencyCode, DateTimeOffset now) => new(
        quote.Id, quote.DeliveryType.ToString(), quote.OrderType.ToString(), quote.ScheduledPickupAtUtc,
        new(quote.VehicleCategoryId, quote.VehicleCategoryCode, quote.VehicleCategoryName), quote.DistanceMeters,
        quote.OriginZoneCode, quote.DestinationZoneCode,
        new(quote.Pricing.BaseFee, quote.Pricing.DistanceCharge, quote.Pricing.WeightCharge, quote.Pricing.VehicleCharge, quote.Pricing.ZoneCharge, quote.TotalAmount, currencyCode),
        quote.PaymentResponsibleParty.ToString(), quote.PaymentMethod.ToString(), quote.IssuedAtUtc, quote.ExpiresAtUtc,
        quote.ConsumptionStatus == Domain.Deliveries.Enums.DeliveryQuoteConsumptionStatus.Consumed ? "Consumed" : quote.IsExpired(now) ? "Expired" : "Active",
        QuoteAddressResponse.From(quote.PickupAddress), QuoteAddressResponse.From(quote.DropOffAddress),
        quote.Items.Select(x => new QuoteItemResponse(x.Description, x.Quantity, x.WeightKg)).ToArray());
}
