using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("Countries", SchemaNames.ReferenceData);
        builder.HasKey(country => country.Id);

        builder.Property(country => country.Name).HasMaxLength(150).IsRequired();
        builder.Property(country => country.ShortCode).HasMaxLength(3).IsRequired();
        builder.Property(country => country.CallingCode).HasMaxLength(20);
        builder.Property(country => country.FlagUrl).HasMaxLength(200).IsRequired();

        builder.HasIndex(country => country.ShortCode)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}
