using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using System.Text.Json.Serialization;

namespace BLVK.Pickly.Logistics.Application.Deliveries;

/// <summary>
/// Customer-facing view of the shared location a saved pickup location references.
/// </summary>
public sealed record PickupLocationAddress(
    string DisplayAddress,
    string AddressLine,
    string City,
    string State,
    string CountryCode,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] LocationType LocationType,
    decimal Latitude,
    decimal Longitude);
