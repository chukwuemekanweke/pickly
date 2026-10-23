using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class RiderAvailabilityHistoryConfiguration : IEntityTypeConfiguration<RiderAvailabilityHistory>
{
    public void Configure(EntityTypeBuilder<RiderAvailabilityHistory> builder)
    {
        builder.ToTable("RiderAvailabilityHistory", SchemaNames.Riders);
        builder.HasKey(history => history.Id);
        builder.Property(history => history.RiderProfileId).IsRequired();
        builder.Property(history => history.PreviousStatus).HasConversion<int?>();
        builder.Property(history => history.NewStatus).HasConversion<int>().IsRequired();
        builder.Property(history => history.OccurredAtUtc).IsRequired();
        builder.HasIndex(history => new { history.RiderProfileId, history.OccurredAtUtc, history.Id });

        builder.HasOne<RiderProfile>()
            .WithMany()
            .HasForeignKey(history => history.RiderProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
