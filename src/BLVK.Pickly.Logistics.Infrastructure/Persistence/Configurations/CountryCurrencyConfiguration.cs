using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class CountryCurrencyConfiguration : IEntityTypeConfiguration<CountryCurrency>
{
    public void Configure(EntityTypeBuilder<CountryCurrency> builder)
    {
        builder.ToTable("CountryCurrencies", SchemaNames.Payments);

        builder.HasKey(mapping => mapping.Id);

        builder.Property(mapping => mapping.CountryId).IsRequired();
        builder.Property(mapping => mapping.CurrencyId).IsRequired();
        builder.Property(mapping => mapping.IsDefault).IsRequired();
        builder.Property(mapping => mapping.IsActive).IsRequired();

        builder.HasIndex(mapping => new { mapping.CountryId, mapping.CurrencyId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        builder.HasIndex(mapping => new { mapping.CountryId, mapping.IsDefault })
            .HasFilter("\"IsDeleted\" = FALSE AND \"IsDefault\" = TRUE")
            .IsUnique();
    }
}
