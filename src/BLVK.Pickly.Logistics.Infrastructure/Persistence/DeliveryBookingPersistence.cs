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
    private const string DeliveryQuoteIdIndexName = "IX_Deliveries_DeliveryQuoteId";

    public async Task<DeliveryBookingCommitResult> CommitAsync(
        Delivery delivery,
        DeliveryQuote quote,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Deliveries.AddAsync(delivery, cancellationToken);

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
                return await ResolveConcurrentOutcomeAsync(delivery.SenderId, delivery.DeliveryQuoteId, cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueConstraint(exception, DeliveryQuoteIdIndexName))
            {
                await transaction.RollbackAsync(cancellationToken);
                return await ResolveConcurrentOutcomeAsync(delivery.SenderId, delivery.DeliveryQuoteId, cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        throw new InvalidOperationException("The delivery booking could not be committed.");
    }

    private async Task<DeliveryBookingCommitResult> ResolveConcurrentOutcomeAsync(
        Guid senderId,
        Guid quoteId,
        CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();

        var existingDeliveryExists = await dbContext.Deliveries
            .AsNoTracking()
            .AnyAsync(
                delivery => delivery.SenderId == senderId && delivery.DeliveryQuoteId == quoteId,
                cancellationToken);

        return existingDeliveryExists
            ? new(DeliveryBookingCommitStatus.Existing)
            : new(DeliveryBookingCommitStatus.Conflict);
    }

    private static bool IsUniqueConstraint(DbUpdateException exception, string constraintName) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: var actualConstraintName
        } && actualConstraintName == constraintName;
}
