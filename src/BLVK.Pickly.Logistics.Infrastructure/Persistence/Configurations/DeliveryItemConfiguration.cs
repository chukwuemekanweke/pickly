using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class DeliveryItemConfiguration : IEntityTypeConfiguration<DeliveryItem>
{
    public void Configure(EntityTypeBuilder<DeliveryItem> builder)
    {
        builder.ToTable("DeliveryItems", SchemaNames.Deliveries);
        builder.HasKey(item => item.Id);
        builder.Property(item => item.DeliveryId).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(500).IsRequired();
        builder.Property(item => item.Quantity).IsRequired();
        builder.Property(item => item.WeightKg).HasPrecision(10, 3).IsRequired();
        builder.HasIndex(item => item.DeliveryId);
    }
}
