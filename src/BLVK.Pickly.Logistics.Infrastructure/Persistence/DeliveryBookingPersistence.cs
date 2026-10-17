using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public sealed class DeliveryBookingPersistence(
    AppDbContext dbContext,
    IDeliveryTrackingIdGenerator trackingIdGenerator) : IDeliveryBookingPersistence
{
    private const int MaximumTrackingCollisionRetries = 4;
    private const string TrackingIdIndexName = "IX_Deliveries_TrackingId";

    public async Task<DeliveryBookingCommitResult> CommitAsync(
        Delivery delivery,
        DeliveryQuote quote,
        DeliveryBookingConfirmation confirmation,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Deliveries.AddAsync(delivery, cancellationToken);
        await dbContext.DeliveryBookingConfirmations.AddAsync(confirmation, cancellationToken);

        if (dbContext.Entry(quote).State == EntityState.Detached)
        {
            dbContext.DeliveryQuotes.Update(quote);
        }

        for (var attempt = 0; attempt <= MaximumTrackingCollisionRetries; attempt++)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new(DeliveryBookingCommitStatus.Created);
            }
            catch (DbUpdateException exception) when (
                IsUniqueConstraint(exception, TrackingIdIndexName) &&
                attempt < MaximumTrackingCollisionRetries)
            {
                await transaction.RollbackAsync(cancellationToken);
                delivery.ReplaceTrackingIdAfterCollision(trackingIdGenerator.Generate());
            }
            catch (DbUpdateException exception) when (IsUniqueConstraint(exception, TrackingIdIndexName))
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new InvalidOperationException(
                    "A unique delivery tracking ID could not be generated after bounded retries.",
                    exception);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return await ResolveConcurrentOutcomeAsync(confirmation, cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueConstraint(exception))
            {
                await transaction.RollbackAsync(cancellationToken);
                return await ResolveConcurrentOutcomeAsync(confirmation, cancellationToken);
            }
        }
        throw new InvalidOperationException("The delivery booking could not be committed.");
    }

    private async Task<DeliveryBookingCommitResult> ResolveConcurrentOutcomeAsync(
        DeliveryBookingConfirmation attempted,
        CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();

        var existing = await dbContext.DeliveryBookingConfirmations
            .AsNoTracking()
            .Where(confirmation =>
                confirmation.SenderId == attempted.SenderId &&
                (confirmation.DeliveryQuoteId == attempted.DeliveryQuoteId ||
                 confirmation.Operation == attempted.Operation && confirmation.KeyHash == attempted.KeyHash))
            .OrderBy(confirmation => confirmation.DeliveryQuoteId == attempted.DeliveryQuoteId ? 0 : 1)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            return new(DeliveryBookingCommitStatus.Conflict);
        }

        return new(
            DeliveryBookingCommitStatus.Existing,
            existing.DeliveryId,
            existing.RequestFingerprint);
    }

    private static bool IsUniqueConstraint(DbUpdateException exception, string? constraintName = null) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        } postgresException &&
        (constraintName is null || postgresException.ConstraintName == constraintName);
}
