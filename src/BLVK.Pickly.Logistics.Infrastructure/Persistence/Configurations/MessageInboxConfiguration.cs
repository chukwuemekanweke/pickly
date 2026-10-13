using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class MessageInboxConfiguration : IEntityTypeConfiguration<MessageInbox>
{
    public void Configure(EntityTypeBuilder<MessageInbox> builder)
    {
        builder.ToTable("MessageInboxes", SchemaNames.Integration);

        builder.HasKey(inbox => inbox.Id);

        builder.Property(inbox => inbox.MessageType)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(inbox => inbox.ReceivedAtUtc)
            .IsRequired();

        builder.HasIndex(inbox => new { inbox.MessageId, inbox.MessageType })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}
