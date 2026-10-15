using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;

namespace BLVK.Pickly.Logistics.Application.Deliveries;

public sealed record PickupLocationResponse(
    Guid Id,
    string Label,
    PickupLocationAddress Address,
    bool IsDefault)
{
    public static PickupLocationResponse FromEntity(SavedPickupLocation location) =>
        new(
            location.Id,
            location.Label,
            new PickupLocationAddress(
                location.Address.AddressLine,
                location.Address.AdditionalInformation,
                location.Address.City,
                location.Address.State,
                location.Address.CountryCode,
                location.Address.Latitude,
                location.Address.Longitude),
            location.IsDefault);

    public static PickupLocationResponse FromReadModel(SavedPickupLocationReadModel location) =>
        new(
            location.Id,
            location.Label,
            new PickupLocationAddress(
                location.AddressLine,
                location.AdditionalInformation,
                location.City,
                location.State,
                location.CountryCode,
                location.Latitude,
                location.Longitude),
            location.IsDefault);
}
