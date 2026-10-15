namespace BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;

public sealed record SavedPickupLocationReadModel(
    Guid Id,
    string Label,
    string AddressLine,
    string? AdditionalInformation,
    string City,
    string State,
    string CountryCode,
    decimal Latitude,
    decimal Longitude,
    bool IsDefault,
    DateTimeOffset CreatedAtUtc);
