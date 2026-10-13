using BLVK.Pickly.Logistics.Domain.Notifications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class EmailDeliveryWebhookInboxConfiguration : IEntityTypeConfiguration<EmailDeliveryWebhookInbox>
{
    public void Configure(EntityTypeBuilder<EmailDeliveryWebhookInbox> builder)
    {
        builder.ToTable("EmailDeliveryWebhookInboxes", SchemaNames.Notifications);

        builder.HasKey(inbox => inbox.Id);

        builder.Property(inbox => inbox.ProviderId)
            .IsRequired();

        builder.Property(inbox => inbox.WebhookEventId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(inbox => inbox.ProviderMessageId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(inbox => inbox.RecipientEmail)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(inbox => inbox.SendingStream)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(inbox => inbox.SendingDomainName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(inbox => inbox.RawPayload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(inbox => inbox.StatusChangeReason)
            .HasMaxLength(500);

        builder.Property(inbox => inbox.ProcessingError)
            .HasMaxLength(4000);

        builder.Property(inbox => inbox.OccurredAtUtc)
            .IsRequired();

        builder.Property(inbox => inbox.ReceivedAtUtc)
            .IsRequired();

        builder.HasIndex(inbox => inbox.WebhookProcessingStatus);

        builder.HasIndex(inbox => new { inbox.ProviderId, inbox.WebhookEventId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        builder.HasIndex(inbox => inbox.ProviderMessageId);
    }
}
