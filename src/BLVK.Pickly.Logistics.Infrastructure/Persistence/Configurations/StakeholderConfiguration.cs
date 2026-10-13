using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class StakeholderConfiguration : IEntityTypeConfiguration<Stakeholder>
{
    public void Configure(EntityTypeBuilder<Stakeholder> builder)
    {
        builder.ToTable("Stakeholders", SchemaNames.Stakeholders);
        builder.HasKey(stakeholder => stakeholder.Id);

        builder.Property(stakeholder => stakeholder.AppUserId).IsRequired();
        builder.Property(stakeholder => stakeholder.TenantId).IsRequired();
        builder.Property(stakeholder => stakeholder.CountryId).IsRequired();
        builder.Property(stakeholder => stakeholder.StakeholderTypeId).IsRequired();
        builder.Property(stakeholder => stakeholder.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(stakeholder => stakeholder.LastName).HasMaxLength(100).IsRequired();
        builder.Property(stakeholder => stakeholder.AvatarUrl).HasMaxLength(2048);
        builder.Property(stakeholder => stakeholder.Theme).HasMaxLength(16).IsRequired();
        builder.Property(stakeholder => stakeholder.Language).HasMaxLength(35).IsRequired();
        builder.Property(stakeholder => stakeholder.IsVerified).IsRequired();

        builder.HasIndex(stakeholder => stakeholder.AppUserId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
        builder.HasIndex(stakeholder => stakeholder.TenantId);
        builder.HasIndex(stakeholder => stakeholder.CountryId);
        builder.HasIndex(stakeholder => stakeholder.StakeholderTypeId);

        builder.HasOne(stakeholder => stakeholder.AppUser)
            .WithOne()
            .HasForeignKey<Stakeholder>(stakeholder => stakeholder.AppUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(stakeholder => stakeholder.StakeholderType)
            .WithMany(stakeholderType => stakeholderType.Stakeholders)
            .HasForeignKey(stakeholder => stakeholder.StakeholderTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
