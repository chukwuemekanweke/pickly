using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class DeliveryMatchingSessionConfiguration : IEntityTypeConfiguration<DeliveryMatchingSession>
{
    public void Configure(EntityTypeBuilder<DeliveryMatchingSession> builder)
    {
        builder.ToTable("DeliveryMatchingSessions", SchemaNames.Riders);
        builder.HasKey(session => session.Id);
        builder.Property(session => session.DeliveryId).IsRequired();
        builder.Property(session => session.Status).HasConversion<int>().IsRequired();
        builder.Property(session => session.SearchRadiusMeters).IsRequired();
        builder.Property(session => session.AttemptCount).IsRequired();
        builder.Property(session => session.NextAttemptAtUtc).IsRequired();
        builder.HasIndex(session => session.DeliveryId)
            .HasDatabaseName("IX_DeliveryMatchingSessions_DeliveryId")
            .IsUnique()
            .HasFilter("\"Status\" = 1 AND \"IsDeleted\" = FALSE");
        builder.HasIndex(session => new { session.Status, session.NextAttemptAtUtc })
            .HasFilter("\"IsDeleted\" = FALSE");

        builder.HasOne<Delivery>()
            .WithMany()
            .HasForeignKey(session => session.DeliveryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
