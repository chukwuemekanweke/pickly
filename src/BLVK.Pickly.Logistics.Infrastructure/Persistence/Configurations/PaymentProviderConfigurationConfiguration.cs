using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class PaymentProviderConfigurationConfiguration : IEntityTypeConfiguration<PaymentProviderConfiguration>
{
    public void Configure(EntityTypeBuilder<PaymentProviderConfiguration> builder)
    {
        builder.ToTable("PaymentProviderConfigurations", SchemaNames.Payments);

        builder.HasKey(configuration => configuration.Id);

        builder.Property(configuration => configuration.PaymentProviderId)
            .IsRequired();

        builder.Property(configuration => configuration.PaymentIntent)
            .IsRequired();

        builder.Property(configuration => configuration.PaymentMethodType)
            .IsRequired();

        builder.Property(configuration => configuration.IsEnabled)
            .IsRequired();

        builder.HasOne<PaymentProvider>()
            .WithMany(provider => provider.Configurations)
            .HasForeignKey(configuration => configuration.PaymentProviderId);

        builder.HasIndex(configuration => new
            {
                configuration.PaymentProviderId,
                configuration.CurrencyId,
                configuration.PaymentIntent
            })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}
