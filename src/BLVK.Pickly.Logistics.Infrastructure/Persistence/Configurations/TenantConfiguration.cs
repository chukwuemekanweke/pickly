using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants", SchemaNames.Stakeholders);
        builder.HasKey(tenant => tenant.Id);

        builder.Property(tenant => tenant.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(tenant => tenant.BrandKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(tenant => tenant.BrandKey)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}
