using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class RiderDeliveryOfferConfiguration : IEntityTypeConfiguration<RiderDeliveryOffer>
{
    public void Configure(EntityTypeBuilder<RiderDeliveryOffer> builder)
    {
        builder.ToTable("RiderDeliveryOffers", SchemaNames.Riders);
        builder.HasKey(offer => offer.Id);
        builder.Property(offer => offer.MatchingSessionId).IsRequired();
        builder.Property(offer => offer.DeliveryId).IsRequired();
        builder.Property(offer => offer.RiderProfileId).IsRequired();
        builder.Property(offer => offer.Status).HasConversion<int>().IsRequired();
        builder.Property(offer => offer.OfferedAtUtc).IsRequired();
        builder.Property(offer => offer.ExpiresAtUtc).IsRequired();
        builder.HasIndex(offer => offer.DeliveryId)
            .IsUnique()
            .HasFilter("\"Status\" = 1 AND \"IsDeleted\" = FALSE");
        builder.HasIndex(offer => offer.RiderProfileId)
            .IsUnique()
            .HasFilter("\"Status\" = 1 AND \"IsDeleted\" = FALSE");
        builder.HasIndex(offer => new { offer.Status, offer.ExpiresAtUtc })
            .HasFilter("\"IsDeleted\" = FALSE");

        builder.HasOne<DeliveryMatchingSession>()
            .WithMany()
            .HasForeignKey(offer => offer.MatchingSessionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Delivery>()
            .WithMany()
            .HasForeignKey(offer => offer.DeliveryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderProfile>()
            .WithMany()
            .HasForeignKey(offer => offer.RiderProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
