using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class DeliveryPaymentAttemptConfiguration : IEntityTypeConfiguration<DeliveryPaymentAttempt>
{
    public void Configure(EntityTypeBuilder<DeliveryPaymentAttempt> builder)
    {
        builder.ToTable("DeliveryPaymentAttempts", SchemaNames.Deliveries);
        builder.HasKey(attempt => attempt.Id);
        builder.Property(attempt => attempt.DeliveryId).IsRequired();
        builder.Property(attempt => attempt.PaymentTransactionId).IsRequired();
        builder.Property(attempt => attempt.IdempotencyKey).HasMaxLength(200);
        builder.Property(attempt => attempt.ApplicationException).HasMaxLength(500);

        builder.HasIndex(attempt => attempt.DeliveryId);
        builder.HasIndex(attempt => attempt.PaymentTransactionId).IsUnique();
        builder.HasIndex(attempt => new { attempt.DeliveryId, attempt.IdempotencyKey })
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL AND \"IsDeleted\" = FALSE");

        builder.HasOne<Delivery>()
            .WithMany()
            .HasForeignKey(attempt => attempt.DeliveryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PaymentTransaction>()
            .WithMany()
            .HasForeignKey(attempt => attempt.PaymentTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
