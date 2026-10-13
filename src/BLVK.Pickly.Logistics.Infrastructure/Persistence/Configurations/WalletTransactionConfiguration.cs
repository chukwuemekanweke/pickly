using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable("WalletTransactions", SchemaNames.Payments);

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.MerchantReference)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(transaction => transaction.TransactionTitle)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(transaction => transaction.Description)
            .HasMaxLength(1000);

        builder.Property(transaction => transaction.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(transaction => transaction.PaymentTransactionId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        builder.HasIndex(transaction => new { transaction.CreatedAtUtc, transaction.Id })
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}
