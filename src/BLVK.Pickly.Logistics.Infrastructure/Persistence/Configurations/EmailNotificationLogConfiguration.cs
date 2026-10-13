using BLVK.Pickly.Logistics.Domain.Notifications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class EmailNotificationLogConfiguration : IEntityTypeConfiguration<EmailNotificationLog>
{
    public void Configure(EntityTypeBuilder<EmailNotificationLog> builder)
    {
        builder.ToTable("EmailNotificationLogs", SchemaNames.Notifications);

        builder.HasKey(log => log.Id);

        builder.Property(log => log.MessageId)
            .IsRequired();

        builder.Property(log => log.TenantId)
            .IsRequired();

        builder.Property(log => log.CountryId)
            .IsRequired();

        builder.Property(log => log.NotificationType)
            .IsRequired();

        builder.Property(log => log.Language)
            .HasMaxLength(35)
            .IsRequired();

        builder.Property(log => log.NotificationContent)
            .HasConversion(GetNotificationContentConverter())
            .Metadata.SetValueComparer(GetNotificationContentComparer());
        builder.Property(log => log.NotificationContent)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(log => log.To)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(log => log.Cc)
            .HasMaxLength(4000);

        builder.Property(log => log.Bcc)
            .HasMaxLength(4000);

        builder.Property(log => log.ProviderMessageId)
            .HasMaxLength(200);

        builder.Property(log => log.EnqueuedAtUtc)
            .IsRequired();

        builder.Property(log => log.SentAtUtc);

        builder.Property(log => log.DeliveredAtUtc);

        builder.Property(log => log.FailureReason)
            .HasMaxLength(4000);

        builder.HasIndex(log => log.MessageId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        builder.HasIndex(log => log.ProviderMessageId)
            .IsUnique()
            .HasFilter("\"ProviderMessageId\" IS NOT NULL AND \"IsDeleted\" = FALSE");
    }

    private static ValueConverter<Dictionary<string, string>, string> GetNotificationContentConverter() =>
        new(
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
            value => JsonSerializer.Deserialize<Dictionary<string, string>>(value, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());

    private static ValueComparer<Dictionary<string, string>> GetNotificationContentComparer() =>
        new(
            (left, right) => left!.OrderBy(pair => pair.Key).SequenceEqual(right!.OrderBy(pair => pair.Key)),
            dictionary => dictionary
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Aggregate(0, (hash, pair) => HashCode.Combine(hash, pair.Key, pair.Value)),
            dictionary => dictionary.ToDictionary(pair => pair.Key, pair => pair.Value));
}
