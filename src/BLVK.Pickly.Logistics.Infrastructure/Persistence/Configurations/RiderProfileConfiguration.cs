using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class RiderProfileConfiguration : IEntityTypeConfiguration<RiderProfile>
{
    public void Configure(EntityTypeBuilder<RiderProfile> builder)
    {
        builder.ToTable("RiderProfiles", SchemaNames.Riders);
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.StakeholderId).IsRequired();
        builder.Property(profile => profile.TenantId).IsRequired();
        builder.Property(profile => profile.VehicleCategoryId).IsRequired();
        builder.Property(profile => profile.AvailabilityStatus).HasConversion<int>().IsRequired();
        builder.Property(profile => profile.VerificationStatus).HasConversion<int>().IsRequired();
        builder.Ignore(profile => profile.IsApproved);
        builder.Ignore(profile => profile.HasEnabledAvailability);

        builder.HasIndex(profile => profile.StakeholderId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
        builder.HasIndex(profile => new { profile.TenantId, profile.AvailabilityStatus, profile.VehicleCategoryId })
            .HasFilter("\"IsDeleted\" = FALSE");

        builder.HasOne<Stakeholder>()
            .WithMany()
            .HasForeignKey(profile => profile.StakeholderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleCategory>()
            .WithMany()
            .HasForeignKey(profile => profile.VehicleCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(profile => profile.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
