using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
        builder.ToTable("Deliveries", SchemaNames.Deliveries);
        builder.HasKey(delivery => delivery.Id);

        builder.Property(delivery => delivery.TenantId).IsRequired();
        builder.Property(delivery => delivery.SenderId).IsRequired();
        builder.Property(delivery => delivery.TrackingId).HasMaxLength(40).IsRequired();
        builder.Property(delivery => delivery.DeliveryType).HasConversion<int>().IsRequired();
        builder.Property(delivery => delivery.OrderType).HasConversion<int>().IsRequired();
        builder.Property(delivery => delivery.Status).HasConversion<int>().IsRequired();
        builder.Property(delivery => delivery.PaymentStatus).HasConversion<int>().IsRequired();

        builder.HasIndex(delivery => delivery.TrackingId).IsUnique();
        builder.HasIndex(delivery => delivery.PaymentTransactionId)
            .IsUnique()
            .HasFilter("\"PaymentTransactionId\" IS NOT NULL");
        builder.HasIndex(delivery => delivery.CashCollectionId)
            .IsUnique()
            .HasFilter("\"CashCollectionId\" IS NOT NULL");
        builder.HasIndex(delivery => delivery.ProofOfDeliveryId)
            .IsUnique()
            .HasFilter("\"ProofOfDeliveryId\" IS NOT NULL");
        builder.HasIndex(delivery => new { delivery.TenantId, delivery.SenderId, delivery.CreatedAtUtc })
            .HasFilter("\"IsDeleted\" = FALSE");
        builder.HasIndex(delivery => new { delivery.Status, delivery.DispatchEligibleAtUtc })
            .HasFilter("\"IsDeleted\" = FALSE");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(delivery => delivery.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Stakeholder>()
            .WithMany()
            .HasForeignKey(delivery => delivery.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(delivery => delivery.PickupAddress, owned => ConfigureAddress(owned, "Pickup"));
        builder.OwnsOne(delivery => delivery.DropOffAddress, owned => ConfigureAddress(owned, "DropOff"));

        builder.OwnsOne(delivery => delivery.Recipient, owned =>
        {
            owned.Property(recipient => recipient.Name)
                .HasColumnName("RecipientName")
                .HasMaxLength(200)
                .IsRequired();
            owned.Property(recipient => recipient.PhoneNumber)
                .HasColumnName("RecipientPhoneNumber")
                .HasMaxLength(16)
                .IsRequired();
        });

        builder.OwnsOne(delivery => delivery.Charge, owned =>
        {
            owned.Property(charge => charge.Amount)
                .HasColumnName("ChargeAmount")
                .HasPrecision(18, 2)
                .IsRequired();
            owned.Property(charge => charge.CurrencyId)
                .HasColumnName("CurrencyId")
                .IsRequired();
            owned.HasOne<Currency>()
                .WithMany()
                .HasForeignKey(charge => charge.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.HasMany(delivery => delivery.Items)
            .WithOne()
            .HasForeignKey(item => item.DeliveryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(delivery => delivery.StatusHistory)
            .WithOne()
            .HasForeignKey(history => history.DeliveryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(delivery => delivery.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(delivery => delivery.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureAddress(
        OwnedNavigationBuilder<Delivery, DeliveryAddress> owned,
        string prefix)
    {
        owned.Property(address => address.AddressLine)
            .HasColumnName($"{prefix}AddressLine")
            .HasMaxLength(300)
            .IsRequired();
        owned.Property(address => address.AdditionalInformation)
            .HasColumnName($"{prefix}AdditionalInformation")
            .HasMaxLength(500);
        owned.Property(address => address.City)
            .HasColumnName($"{prefix}City")
            .HasMaxLength(100)
            .IsRequired();
        owned.Property(address => address.State)
            .HasColumnName($"{prefix}State")
            .HasMaxLength(100)
            .IsRequired();
        owned.Property(address => address.CountryCode)
            .HasColumnName($"{prefix}CountryCode")
            .HasMaxLength(2)
            .IsFixedLength()
            .IsRequired();
        owned.Property(address => address.Latitude)
            .HasColumnName($"{prefix}Latitude")
            .HasPrecision(9, 6)
            .IsRequired();
        owned.Property(address => address.Longitude)
            .HasColumnName($"{prefix}Longitude")
            .HasPrecision(9, 6)
            .IsRequired();
    }
}
