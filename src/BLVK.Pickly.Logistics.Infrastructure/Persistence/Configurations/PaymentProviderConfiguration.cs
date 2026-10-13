using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class PaymentProviderEntityConfiguration : IEntityTypeConfiguration<PaymentProvider>
{
    public void Configure(EntityTypeBuilder<PaymentProvider> builder)
    {
        builder.ToTable("PaymentProviders", SchemaNames.Payments);

        builder.HasKey(provider => provider.Id);

        builder.Property(provider => provider.ProviderName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(provider => provider.ProviderKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(provider => provider.IsActive)
            .IsRequired();

        builder.HasMany(provider => provider.Configurations)
            .WithOne()
            .HasForeignKey(configuration => configuration.PaymentProviderId);

        builder.HasIndex(provider => provider.ProviderKey)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}
