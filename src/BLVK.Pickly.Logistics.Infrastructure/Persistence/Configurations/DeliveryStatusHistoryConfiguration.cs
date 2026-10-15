using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class DeliveryStatusHistoryConfiguration : IEntityTypeConfiguration<DeliveryStatusHistory>
{
    public void Configure(EntityTypeBuilder<DeliveryStatusHistory> builder)
    {
        builder.ToTable("DeliveryStatusHistory", SchemaNames.Deliveries);
        builder.HasKey(history => history.Id);
        builder.Property(history => history.DeliveryId).IsRequired();
        builder.Property(history => history.PreviousStatus).HasConversion<int?>();
        builder.Property(history => history.NewStatus).HasConversion<int>().IsRequired();
        builder.Property(history => history.Reason).HasMaxLength(500);
        builder.Property(history => history.OccurredAtUtc).IsRequired();
        builder.Property(history => history.Actor).HasMaxLength(200).IsRequired();
        builder.Property(history => history.Source).HasMaxLength(100).IsRequired();
        builder.HasIndex(history => new { history.DeliveryId, history.OccurredAtUtc, history.Id });
    }
}
