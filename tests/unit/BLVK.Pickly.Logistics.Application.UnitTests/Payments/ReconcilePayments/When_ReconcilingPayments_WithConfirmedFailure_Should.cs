using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Payments.ReconcilePayments;

public sealed class When_ReconcilingPayments_WithConfirmedFailure_Should
{
    [Fact]
    public async Task EnqueueOneDeliveryPaymentFailureNotification()
    {
        var context = new PaymentsFlowTestContext();
        var currency = context.CreateCurrency("NGN");
        var provider = context.CreatePaymentProvider("Credo", PaymentProviderKeys.Credo);
        var providerService = Substitute.For<IPaymentProviderService>();
        var senderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var transaction = PaymentTransaction.Create(
            "merchant-failed",
            PaymentIntent.DeliveryPayment,
            provider.Id,
            2500m,
            currency.Id,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            senderId,
            tenantId);
        transaction.MarkInitiated(
            "provider-ref",
            null,
            null,
            KnownPaymentTransactionChangeReasons.PaymentInitiated);
        var delivery = Delivery.Create(
            senderId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "PKL-FAILED-PAYMENT",
            DeliveryType.Instant,
            DeliveryOrderType.PayBeforeDelivery,
            DeliveryAddress.Create("1 Pickup", null, "Lagos", "Lagos", "NG", 6.5m, 3.3m),
            DeliveryAddress.Create("2 Dropoff", null, "Lagos", "Lagos", "NG", 6.6m, 3.4m),
            DeliveryRecipient.Create("Recipient", "+2348012345678"),
            DeliveryCharge.Create(2500m, currency.Id),
            [DeliveryItem.Create("Parcel", 1, 1m)],
            context.Clock.GetUtcNow());
        var attempt = DeliveryPaymentAttempt.Create(delivery.Id, transaction.Id, "failure-attempt");

        context.PaymentTransactionRepository.ListAsync(
                Arg.Any<ISpecification<PaymentTransaction>>(),
                Arg.Any<CancellationToken>())
            .Returns([transaction]);
        context.CurrencyRepository.GetByIdAsync(currency.Id, Arg.Any<CancellationToken>()).Returns(currency);
        context.PaymentProviderRepository.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);
        context.DeliveryPaymentAttemptRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<DeliveryPaymentAttempt>>(),
                Arg.Any<CancellationToken>())
            .Returns(attempt);
        context.DeliveryRepository.GetByIdAsync(delivery.Id, Arg.Any<CancellationToken>()).Returns(delivery);
        context.StakeholderReadModelRepository.GetByStakeholderIdAsync(senderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(
                senderId,
                Guid.CreateVersion7(),
                "sender@example.com",
                tenantId,
                countryId,
                Guid.CreateVersion7(),
                "Ada",
                "Sender",
                null,
                true));
        providerService.ProviderKey.Returns(PaymentProviderKeys.Credo);
        providerService.VerifyPaymentAsync(
                Arg.Any<PaymentProviderVerificationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Failed,
                "provider-ref",
                "provider_declined",
                KnownPaymentTransactionChangeReasons.ReconciliationConfirmedFailure));
        context.PaymentProviderServices.Add(providerService);

        await context.CreatePaymentReconciliationService().HandleAsync(
            context.Clock.GetUtcNow().AddHours(-48),
            context.Clock.GetUtcNow().AddMinutes(-10),
            context.Clock.GetUtcNow().AddMinutes(-2),
            50,
            CancellationToken.None);

        transaction.PaymentStatus.ShouldBe(PaymentStatus.Failed);
        await context.CommandSender.Received(1).SendAsync(
            Arg.Is<SendNotificationCommand>(notification =>
                notification.NotificationType == NotificationType.PaymentFailed &&
                notification.StakeholderId == senderId),
            Arg.Any<CancellationToken>());
        await context.EventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<SuccessfulPaymentConfirmed>(),
            Arg.Any<CancellationToken>());
    }
}
