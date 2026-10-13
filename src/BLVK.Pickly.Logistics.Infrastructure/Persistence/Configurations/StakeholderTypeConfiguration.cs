using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class StakeholderTypeConfiguration : IEntityTypeConfiguration<StakeholderType>
{
    public void Configure(EntityTypeBuilder<StakeholderType> builder)
    {
        builder.ToTable("StakeholderTypes", SchemaNames.Stakeholders);
        builder.HasKey(stakeholderType => stakeholderType.Id);

        builder.Property(stakeholderType => stakeholderType.TenantId).IsRequired();
        builder.Property(stakeholderType => stakeholderType.Name).HasMaxLength(150).IsRequired();
        builder.Property(stakeholderType => stakeholderType.Key).HasMaxLength(100).IsRequired();

        builder.HasIndex(stakeholderType => stakeholderType.TenantId);
        builder.HasIndex(stakeholderType => new { stakeholderType.TenantId, stakeholderType.Key })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}
