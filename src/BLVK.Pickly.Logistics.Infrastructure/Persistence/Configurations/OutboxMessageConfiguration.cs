using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages", SchemaNames.Integration);
        builder.HasIndex(message => message.MessageId).IsUnique();
        builder.HasIndex(message => new { message.SentAtUtc, message.AvailableAtUtc });
        builder.Property(message => message.Type).HasMaxLength(500).IsRequired();
        builder.Property(message => message.Payload).IsRequired();
        builder.Property(message => message.CorrelationId).HasMaxLength(128);
        builder.Property(message => message.ActivityId).HasMaxLength(128);
        builder.Property(message => message.LastError).HasMaxLength(4000);
        builder.Property(message => message.AvailableAtUtc).IsRequired();
    }
}
