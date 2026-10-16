namespace BLVK.Pickly.Logistics.Domain.Locations.Services;

/// <summary>
/// Provider-neutral precision of a geocoded feature. Only canonical streets and distinct landmarks may be registered.
/// </summary>
public enum GeocodingPrecision
{
    Premise = 1,
    Street = 2,
    Landmark = 3,
    Area = 4,
    Region = 5
}
