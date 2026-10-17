using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.CreateDelivery;

public sealed class CreateDeliveryHandler(
    IRepository<DeliveryQuote> quotes,
    IRepository<Delivery> deliveries,
    IRepository<DeliveryBookingConfirmation> confirmations,
    IRepository<Currency> currencies,
    IDeliveryBookingPersistence bookingPersistence,
    IDeliveryTrackingIdGenerator trackingIdGenerator,
    IOptions<DeliveryDispatchOptions> options,
    TimeProvider timeProvider)
{
    public async Task<CreateDeliveryResult> HandleAsync(
        CreateDeliveryCommand command,
        CancellationToken cancellationToken)
    {
        if (!command.ActorContext.StakeholderId.HasValue || command.ActorContext.StakeholderId == Guid.Empty)
        {
            return new(CreateDeliveryStatus.NotAuthenticated);
        }

        if (command.QuoteId == Guid.Empty)
        {
            return new(CreateDeliveryStatus.InvalidRequest);
        }

        if (command.IdempotencyKey is { Length: > 200 } ||
            command.IdempotencyKey is not null && string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return new(CreateDeliveryStatus.InvalidIdempotencyKey);
        }

        DeliveryRecipient recipient;
        DeliveryAddress dropOffAddress;
        try
        {
            recipient = DeliveryRecipient.Create(command.RecipientName, command.RecipientPhoneNumber);
        }
        catch (ArgumentException)
        {
            return new(CreateDeliveryStatus.InvalidRequest);
        }

        var senderId = command.ActorContext.StakeholderId.Value;
        var normalizedInstructions = string.IsNullOrWhiteSpace(command.DropOffInstructions)
            ? null
            : command.DropOffInstructions.Trim();
        var keyHash = Hash(command.IdempotencyKey?.Trim() ?? $"quote:{command.QuoteId:D}");
        var requestFingerprint = Hash(string.Join(
            '\u001F',
            command.QuoteId.ToString("D"),
            recipient.Name,
            recipient.PhoneNumber,
            normalizedInstructions ?? string.Empty));

        var replay = await ResolveExistingAsync(senderId, keyHash, command.QuoteId, requestFingerprint, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        var quote = await quotes.FirstOrDefaultAsync(
            new TrackedDeliveryQuoteByOwnerAndIdSpecification(senderId, command.QuoteId),
            cancellationToken);
        if (quote is null)
        {
            return new(CreateDeliveryStatus.NotFound);
        }

        var now = timeProvider.GetUtcNow();
        if (quote.ConsumptionStatus == DeliveryQuoteConsumptionStatus.Consumed)
        {
            return new(CreateDeliveryStatus.QuoteConsumed);
        }

        if (quote.IsExpired(now))
        {
            return new(CreateDeliveryStatus.QuoteExpired);
        }

        if (quote.DeliveryType == DeliveryType.Scheduled && quote.ScheduledPickupAtUtc <= now)
        {
            return new(CreateDeliveryStatus.ScheduledPickupExpired);
        }

        try
        {
            dropOffAddress = CopyAddress(
                quote.DropOffAddress,
                normalizedInstructions ?? quote.DropOffAddress.AdditionalInformation);
        }
        catch (ArgumentException)
        {
            return new(CreateDeliveryStatus.InvalidRequest);
        }

        DateTimeOffset? dispatchEligibleAtUtc = quote.DeliveryType == DeliveryType.Scheduled
            ? Max(now, quote.ScheduledPickupAtUtc!.Value - options.Value.LeadTime)
            : null;
        var delivery = Delivery.Create(
            senderId,
            quote.Id,
            quote.VehicleCategoryId,
            trackingIdGenerator.Generate(),
            quote.DeliveryType,
            quote.OrderType,
            CopyAddress(quote.PickupAddress, quote.PickupAddress.AdditionalInformation),
            dropOffAddress,
            recipient,
            DeliveryCharge.Create(quote.TotalAmount, quote.CurrencyId),
            quote.Items.Select(item => DeliveryItem.Create(item.Description, item.Quantity, item.WeightKg)),
            now,
            quote.ScheduledPickupAtUtc,
            dispatchEligibleAtUtc);

        // The aggregate identifier remains stable through any tracking-ID collision retry.
        quote.Consume(delivery.Id, now);
        var confirmation = DeliveryBookingConfirmation.Create(
            senderId,
            quote.Id,
            delivery.Id,
            keyHash,
            requestFingerprint,
            now + options.Value.IdempotencyRetention);

        var commit = await bookingPersistence.CommitAsync(delivery, quote, confirmation, cancellationToken);
        if (commit.Status == DeliveryBookingCommitStatus.Existing && commit.ExistingDeliveryId.HasValue)
        {
            return await ExistingResultAsync(
                senderId,
                commit.ExistingDeliveryId.Value,
                requestFingerprint,
                commit.ExistingRequestFingerprint,
                cancellationToken);
        }

        if (commit.Status != DeliveryBookingCommitStatus.Created)
        {
            return new(CreateDeliveryStatus.PersistenceConflict);
        }

        var currency = await currencies.GetByIdAsync(quote.CurrencyId, cancellationToken);
        return new(CreateDeliveryStatus.Success, DeliveryResponse.From(delivery, currency?.CurrencyCode ?? string.Empty));
    }

    private async Task<CreateDeliveryResult?> ResolveExistingAsync(
        Guid senderId,
        string keyHash,
        Guid quoteId,
        string requestFingerprint,
        CancellationToken cancellationToken)
    {
        var byKey = await confirmations.FirstOrDefaultAsync(
            new DeliveryBookingByKeySpecification(senderId, keyHash), cancellationToken);
        if (byKey is not null)
        {
            return await ExistingResultAsync(
                senderId, byKey.DeliveryId, requestFingerprint, byKey.RequestFingerprint, cancellationToken);
        }

        var byQuote = await confirmations.FirstOrDefaultAsync(
            new DeliveryBookingByQuoteSpecification(senderId, quoteId), cancellationToken);
        return byQuote is null
            ? null
            : await ExistingResultAsync(
                senderId, byQuote.DeliveryId, requestFingerprint, byQuote.RequestFingerprint, cancellationToken);
    }

    private async Task<CreateDeliveryResult> ExistingResultAsync(
        Guid senderId,
        Guid deliveryId,
        string requestFingerprint,
        string? existingFingerprint,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(requestFingerprint, existingFingerprint, StringComparison.Ordinal))
        {
            return new(CreateDeliveryStatus.IdempotencyConflict);
        }

        var delivery = await deliveries.FirstOrDefaultAsync(
            new DeliveryByOwnerAndIdSpecification(senderId, deliveryId), cancellationToken);
        if (delivery is null)
        {
            return new(CreateDeliveryStatus.PersistenceConflict);
        }

        var currency = await currencies.GetByIdAsync(delivery.Charge.CurrencyId, cancellationToken);
        return new(
            CreateDeliveryStatus.Success,
            DeliveryResponse.From(delivery, currency?.CurrencyCode ?? string.Empty),
            IsReplay: true);
    }

    private static DeliveryAddress CopyAddress(DeliveryAddress source, string? additionalInformation) =>
        DeliveryAddress.Create(
            source.AddressLine,
            additionalInformation,
            source.City,
            source.State,
            source.CountryCode,
            source.Latitude,
            source.Longitude);

    private static DateTimeOffset Max(DateTimeOffset first, DateTimeOffset second) => first >= second ? first : second;

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
