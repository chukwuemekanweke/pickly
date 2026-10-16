using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class VehicleCategoryConfiguration : IEntityTypeConfiguration<VehicleCategory>
{
    public void Configure(EntityTypeBuilder<VehicleCategory> builder)
    {
        builder.ToTable("VehicleCategories", SchemaNames.Deliveries, table => table.HasCheckConstraint("CK_VehicleCategories_MaximumWeightKg", "\"MaximumWeightKg\" > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.MaximumWeightKg).HasPrecision(12, 3).IsRequired();
        builder.Property(x => x.RoutingProfile).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        builder.HasIndex(x => new { x.IsActive, x.MaximumWeightKg }).HasFilter("\"IsDeleted\" = FALSE");
    }
}
