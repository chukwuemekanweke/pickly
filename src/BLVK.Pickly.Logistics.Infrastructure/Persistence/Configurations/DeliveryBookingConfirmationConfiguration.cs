using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class DeliveryBookingConfirmationConfiguration : IEntityTypeConfiguration<DeliveryBookingConfirmation>
{
    public void Configure(EntityTypeBuilder<DeliveryBookingConfirmation> builder)
    {
        builder.ToTable("DeliveryBookingConfirmations", SchemaNames.Deliveries);
        builder.HasKey(confirmation => confirmation.Id);
        builder.Property(confirmation => confirmation.Operation).HasMaxLength(50).IsRequired();
        builder.Property(confirmation => confirmation.KeyHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(confirmation => confirmation.RequestFingerprint).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(confirmation => confirmation.ExpiresAtUtc).IsRequired();

        builder.HasIndex(confirmation => new { confirmation.SenderId, confirmation.Operation, confirmation.KeyHash })
            .IsUnique();
        builder.HasIndex(confirmation => confirmation.DeliveryQuoteId).IsUnique();
        builder.HasIndex(confirmation => confirmation.DeliveryId).IsUnique();
        builder.HasIndex(confirmation => confirmation.ExpiresAtUtc);

        builder.HasOne<Stakeholder>()
            .WithMany()
            .HasForeignKey(confirmation => confirmation.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DeliveryQuote>()
            .WithMany()
            .HasForeignKey(confirmation => confirmation.DeliveryQuoteId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Delivery>()
            .WithMany()
            .HasForeignKey(confirmation => new { confirmation.DeliveryId, confirmation.DeliveryQuoteId })
            .HasPrincipalKey(delivery => new { delivery.Id, delivery.DeliveryQuoteId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
