using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly ValueComparer<Dictionary<string, string>> DictionaryComparer = new(
        (left, right) => Serialize(left) == Serialize(right),
        value => Serialize(value).GetHashCode(),
        value => value.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal));

    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions", SchemaNames.Payments);

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.MerchantReference)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(transaction => transaction.ProviderReference)
            .HasMaxLength(100);

        builder.Property(transaction => transaction.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(transaction => transaction.VerifiedAmount)
            .HasPrecision(18, 2);

        builder.Property(transaction => transaction.FailureReason)
            .HasMaxLength(200);

        builder.Property(transaction => transaction.StatusChangeReason)
            .HasMaxLength(500);

        builder.Property(transaction => transaction.RowVersion)
            .IsRowVersion();

        builder.Property(transaction => transaction.ProviderPayloadMetadata)
            .HasColumnType("jsonb")
            .HasConversion(
                value => Serialize(value),
                value => Deserialize(value))
            .Metadata.SetValueComparer(DictionaryComparer);

        builder.HasIndex(transaction => transaction.MerchantReference)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        builder.HasIndex(transaction => transaction.ProviderReference)
            .HasFilter("\"ProviderReference\" IS NOT NULL AND \"IsDeleted\" = FALSE");

        builder.HasIndex(transaction => transaction.PaymentStatus);
        builder.HasIndex(transaction => transaction.PaymentIntent);
        builder.HasIndex(transaction => transaction.PaymentProviderId);
        builder.HasIndex(transaction => transaction.CreatedAtUtc);
        builder.HasIndex(transaction => transaction.InitiatedByUserId);
    }

    private static string Serialize(Dictionary<string, string>? value) =>
        JsonSerializer.Serialize(value ?? [], JsonSerializerOptions);

    private static Dictionary<string, string> Deserialize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : JsonSerializer.Deserialize<Dictionary<string, string>>(value, JsonSerializerOptions) ?? [];
}
