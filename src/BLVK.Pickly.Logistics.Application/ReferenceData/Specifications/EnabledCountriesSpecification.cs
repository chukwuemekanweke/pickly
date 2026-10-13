using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;

namespace BLVK.Pickly.Logistics.Application.ReferenceData.Specifications;

public sealed class EnabledCountriesSpecification : Specification<Country>
{
    public EnabledCountriesSpecification() => ApplyOrderBy(country => country.Name);
}
