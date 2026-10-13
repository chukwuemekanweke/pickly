using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class PaymentWebhookInboxConfiguration : IEntityTypeConfiguration<PaymentWebhookInbox>
{
    public void Configure(EntityTypeBuilder<PaymentWebhookInbox> builder)
    {
        builder.ToTable("PaymentWebhookInboxes", SchemaNames.Payments);

        builder.HasKey(inbox => inbox.Id);

        builder.Property(inbox => inbox.MerchantReference)
            .HasMaxLength(100);

        builder.Property(inbox => inbox.ProviderReference)
            .HasMaxLength(100);

        builder.Property(inbox => inbox.WebhookEventName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(inbox => inbox.WebhookEventId)
            .HasMaxLength(200);

        builder.Property(inbox => inbox.RawPayload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(inbox => inbox.StatusChangeReason)
            .HasMaxLength(500);

        builder.Property(inbox => inbox.ProcessingError)
            .HasMaxLength(4000);

        builder.Property(inbox => inbox.RowVersion)
            .IsRowVersion();

        builder.HasIndex(inbox => inbox.MerchantReference)
            .HasFilter("\"MerchantReference\" IS NOT NULL AND \"IsDeleted\" = FALSE");

        builder.HasIndex(inbox => inbox.ProviderReference)
            .HasFilter("\"ProviderReference\" IS NOT NULL AND \"IsDeleted\" = FALSE");

        builder.HasIndex(inbox => inbox.WebhookProcessingStatus);

        builder.HasIndex(inbox => new { inbox.PaymentProviderId, inbox.WebhookEventId })
            .IsUnique()
            .HasFilter("\"WebhookEventId\" IS NOT NULL AND \"IsDeleted\" = FALSE");
    }
}
