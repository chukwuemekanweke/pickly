using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public sealed class DeliveryPaymentAttemptReadModelRepository(
    AppDbContext writeDbContext,
    AppReadDbContext readDbContext) : IDeliveryPaymentAttemptReadModelRepository
{
    public async Task<IReadOnlyList<DeliveryPaymentAttemptReadModel>> ListForDecisionAsync(
        Guid deliveryId,
        CancellationToken cancellationToken) =>
        await Project(writeDbContext, deliveryId, null)
            .ToListAsync(cancellationToken);

    public Task<DeliveryPaymentAttemptReadModel?> GetForDecisionAsync(
        Guid paymentTransactionId,
        CancellationToken cancellationToken) =>
        Project(writeDbContext, null, paymentTransactionId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<DeliveryPaymentAttemptReadModel>> ListHistoryAsync(
        Guid deliveryId,
        CancellationToken cancellationToken) =>
        await Project(readDbContext, deliveryId, null)
            .ToListAsync(cancellationToken);

    private static IQueryable<DeliveryPaymentAttemptReadModel> Project<TContext>(
        AppDbContextBase<TContext> dbContext,
        Guid? deliveryId,
        Guid? paymentTransactionId)
        where TContext : DbContext
    {
        var attempts = dbContext.DeliveryPaymentAttempts.AsNoTracking();
        if (deliveryId.HasValue)
        {
            attempts = attempts.Where(attempt => attempt.DeliveryId == deliveryId.Value);
        }
        if (paymentTransactionId.HasValue)
        {
            attempts = attempts.Where(attempt => attempt.PaymentTransactionId == paymentTransactionId.Value);
        }

        return attempts
            .Join(
                dbContext.PaymentTransactions.AsNoTracking(),
                attempt => attempt.PaymentTransactionId,
                payment => payment.Id,
                (attempt, payment) => new { attempt, payment })
            .GroupJoin(
                dbContext.PaymentProviders.AsNoTracking(),
                pair => pair.payment.PaymentProviderId,
                provider => provider.Id,
                (pair, providers) => new { pair.attempt, pair.payment, providers })
            .SelectMany(
                pair => pair.providers.DefaultIfEmpty(),
                (pair, provider) => new { pair.attempt, pair.payment, provider })
            .GroupJoin(
                dbContext.Currencies.AsNoTracking(),
                pair => pair.payment.CurrencyId,
                currency => currency.Id,
                (pair, currencies) => new { pair.attempt, pair.payment, pair.provider, currencies })
            .SelectMany(
                pair => pair.currencies.DefaultIfEmpty(),
                (pair, currency) => new { pair.attempt, pair.payment, pair.provider, currency })
            .OrderByDescending(pair => pair.payment.CreatedAtUtc)
            .ThenByDescending(pair => pair.attempt.Id)
            .Select(pair => new DeliveryPaymentAttemptReadModel(
                    pair.attempt.Id,
                    pair.attempt.DeliveryId,
                    pair.payment.Id,
                    pair.attempt.IdempotencyKey,
                    pair.attempt.AppliedAtUtc,
                    pair.attempt.ApplicationException,
                    pair.payment.PaymentProviderId,
                    pair.provider == null ? string.Empty : pair.provider.ProviderName,
                    pair.provider == null ? string.Empty : pair.provider.ProviderKey,
                    pair.payment.MerchantReference,
                    pair.payment.PaymentStatus,
                    pair.payment.Amount,
                    pair.payment.CurrencyId,
                    pair.currency == null ? string.Empty : pair.currency.CurrencyCode,
                    pair.payment.VerifiedAmount,
                    pair.payment.VerifiedCurrencyId,
                    pair.payment.VerifiedCurrencyCode,
                    pair.payment.FailureReason,
                    pair.payment.CreatedAtUtc,
                    pair.payment.ExpiresAtUtc,
                    pair.payment.PaymentMethodType,
                    pair.payment.ProviderPayloadMetadata));
    }
}
