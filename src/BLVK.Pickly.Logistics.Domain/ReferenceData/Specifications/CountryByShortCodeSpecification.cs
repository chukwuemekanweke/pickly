using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;

namespace BLVK.Pickly.Logistics.Domain.ReferenceData.Specifications;

public sealed class CountryByShortCodeSpecification : Specification<Country>
{
    public CountryByShortCodeSpecification(string shortCode)
    {
        var normalizedShortCode = shortCode.Trim().ToUpperInvariant();
        Where(country => country.ShortCode == normalizedShortCode);
    }
}
