namespace BLVK.Pickly.Logistics.Domain.Locations.Services;

/// <summary>
/// Provider-neutral precision of a geocoded feature. Only <see cref="Premise"/> and <see cref="Street"/> results may
/// become registered locations; coarser results can only anchor a nearby-landmark suggestion.
/// </summary>
public enum GeocodingPrecision
{
    Premise = 1,
    Street = 2,
    Area = 3,
    Region = 4
}
