using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Locations.Entities;

namespace BLVK.Pickly.Logistics.Domain.Locations.Specifications;

public sealed class LocationByProviderReferenceSpecification : Specification<Location>
{
    public LocationByProviderReferenceSpecification(string provider, string featureId, bool enableTracking = false)
    {
        var normalizedProvider = LocationProviderReference.NormalizeProvider(provider);
        Where(location => location.ProviderReferences.Any(reference =>
            reference.Provider == normalizedProvider && reference.FeatureId == featureId));
        AddInclude(location => location.ProviderReferences);
        AddInclude(location => location.ResolutionKeys);

        if (enableTracking)
        {
            EnableTracking();
        }
    }
}
