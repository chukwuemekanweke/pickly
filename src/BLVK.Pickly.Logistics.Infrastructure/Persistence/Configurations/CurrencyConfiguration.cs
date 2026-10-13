using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("Currencies", SchemaNames.Payments);

        builder.HasKey(currency => currency.Id);

        builder.Property(currency => currency.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(currency => currency.CurrencyName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(currency => currency.IsActive)
            .IsRequired();

        builder.HasIndex(currency => currency.CurrencyCode)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}
