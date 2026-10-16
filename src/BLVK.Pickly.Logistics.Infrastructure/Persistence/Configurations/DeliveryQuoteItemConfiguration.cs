using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class DeliveryQuoteItemConfiguration : IEntityTypeConfiguration<DeliveryQuoteItem>
{
    public void Configure(EntityTypeBuilder<DeliveryQuoteItem> builder)
    {
        builder.ToTable("DeliveryQuoteItems", SchemaNames.Deliveries, table => { table.HasCheckConstraint("CK_DeliveryQuoteItems_Quantity", "\"Quantity\" > 0"); table.HasCheckConstraint("CK_DeliveryQuoteItems_WeightKg", "\"WeightKg\" > 0"); });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.WeightKg).HasPrecision(12, 3).IsRequired();
    }
}
