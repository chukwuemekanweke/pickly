using BLVK.Pickly.Logistics.Domain.Providers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToTable("Providers", SchemaNames.Infrastructure);

        builder.HasKey(provider => provider.Id);

        builder.Property(provider => provider.ProviderType)
            .IsRequired();

        builder.Property(provider => provider.ProviderName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(provider => provider.ProviderKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(provider => provider.IsActive)
            .IsRequired();

        builder.HasIndex(provider => new { provider.ProviderType, provider.ProviderKey })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        builder.HasIndex(provider => new { provider.ProviderType, provider.IsActive })
            .HasFilter("\"IsActive\" = TRUE AND \"IsDeleted\" = FALSE")
            .IsUnique();
    }
}
