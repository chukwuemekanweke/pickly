using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;

public sealed class EligibleVehicleCategoriesSpecification : Specification<VehicleCategory>
{
    public EligibleVehicleCategoriesSpecification(decimal totalWeightKg)
    {
        Where(x => x.IsActive && x.MaximumWeightKg >= totalWeightKg);
        ApplyOrderBy(x => x.MaximumWeightKg);
        ApplyThenBy(x => x.Id);
    }
}
