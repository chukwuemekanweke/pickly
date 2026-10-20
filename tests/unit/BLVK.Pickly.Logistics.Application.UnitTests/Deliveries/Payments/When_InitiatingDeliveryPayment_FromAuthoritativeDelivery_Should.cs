using BLVK.Pickly.Logistics.Application.Deliveries.Features.InitiateDeliveryPayment;
using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Deliveries;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.Payments;

public sealed class When_InitiatingDeliveryPayment_FromAuthoritativeDelivery_Should
{
    [Fact]
    public async Task PersistAssociationBeforeReturningProviderInstructions()
    {
        var senderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var currency = Currency.Create("NGN", "Naira", true);
        var stakeholder = Stakeholder.Create(
            Guid.CreateVersion7(), tenantId, countryId, Guid.CreateVersion7(), "Ada", "Sender");
        typeof(Domain.Common.Entities.Entity).GetProperty(nameof(Domain.Common.Entities.Entity.Id))!
            .SetValue(stakeholder, senderId);
        var delivery = CreateDelivery(senderId, currency.Id);
        var provider = PaymentProvider.Create("Test Provider", "test-provider", true);
        provider.SetConfiguration(currency.Id, PaymentIntent.DeliveryPayment, PaymentMethodType.PaymentLink, true);
        var countryCurrency = CountryCurrency.Create(countryId, currency.Id, true, true);
        var deliveryRepository = Substitute.For<IRepository<Delivery>>();
        var attemptRepository = Substitute.For<IRepository<DeliveryPaymentAttempt>>();
        var attemptReadModels = Substitute.For<IDeliveryPaymentAttemptReadModelRepository>();
        var paymentRepository = Substitute.For<IRepository<PaymentTransaction>>();
        var providerRepository = Substitute.For<IRepository<PaymentProvider>>();
        var configurationRepository = Substitute.For<IRepository<PaymentProviderConfiguration>>();
        var currencyRepository = Substitute.For<IRepository<Currency>>();
        var countryCurrencyRepository = Substitute.For<IRepository<CountryCurrency>>();
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var stakeholderReadModels = Substitute.For<IStakeholderReadModelRepository>();
        var consistencyLock = Substitute.For<IDeliveryPaymentInitiationConsistencyLock>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var databaseTransaction = Substitute.For<IUnitOfWorkTransaction>();
        var providerService = Substitute.For<IPaymentProviderService>();
        var commandSender = Substitute.For<ICommandSender>();
        unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(databaseTransaction);
        deliveryRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Delivery>>(), Arg.Any<CancellationToken>()).Returns(delivery);
        deliveryRepository.GetByIdAsync(delivery.Id, Arg.Any<CancellationToken>()).Returns(delivery);
        attemptReadModels.ListForDecisionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        stakeholderRepository.GetByIdAsync(senderId, Arg.Any<CancellationToken>()).Returns(stakeholder);
        stakeholderReadModels.GetByStakeholderIdAsync(senderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(
                senderId,
                stakeholder.AppUserId,
                "sender@example.com",
                tenantId,
                countryId,
                stakeholder.StakeholderTypeId,
                stakeholder.FirstName,
                stakeholder.LastName,
                null,
                true));
        currencyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Currency>>(), Arg.Any<CancellationToken>()).Returns(currency);
        currencyRepository.GetByIdAsync(currency.Id, Arg.Any<CancellationToken>()).Returns(currency);
        countryCurrencyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<CountryCurrency>>(), Arg.Any<CancellationToken>()).Returns(countryCurrency);
        providerRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentProvider>>(), Arg.Any<CancellationToken>()).Returns(provider);
        providerRepository.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);
        configurationRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentProviderConfiguration>>(), Arg.Any<CancellationToken>())
            .Returns(provider.Configurations.Single());
        providerService.ProviderKey.Returns(provider.ProviderKey);
        providerService.InitiatePaymentAsync(Arg.Any<PaymentProviderInitiationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderInitiationResult(
                "provider-reference",
                provider.ProviderKey,
                PaymentMethodType.PaymentLink,
                null,
                new Dictionary<string, string> { ["paymentLink"] = "https://payments.invalid/checkout" }));
        PaymentTransaction? persistedPayment = null;
        DeliveryPaymentAttempt? persistedAttempt = null;
        paymentRepository.When(repository => repository.AddAsync(Arg.Any<PaymentTransaction>(), Arg.Any<CancellationToken>()))
            .Do(call => persistedPayment = call.ArgAt<PaymentTransaction>(0));
        paymentRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => persistedPayment);
        attemptRepository.When(repository => repository.AddAsync(Arg.Any<DeliveryPaymentAttempt>(), Arg.Any<CancellationToken>()))
            .Do(call => persistedAttempt = call.ArgAt<DeliveryPaymentAttempt>(0));
        attemptReadModels.GetForDecisionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => new DeliveryPaymentAttemptReadModel(
                persistedAttempt!.Id,
                delivery.Id,
                persistedPayment!.Id,
                persistedAttempt.IdempotencyKey,
                null,
                null,
                provider.Id,
                provider.ProviderName,
                provider.ProviderKey,
                persistedPayment.MerchantReference,
                persistedPayment.PaymentStatus,
                persistedPayment.Amount,
                currency.Id,
                currency.CurrencyCode,
                null,
                null,
                null,
                null,
                persistedPayment.CreatedAtUtc,
                null,
                persistedPayment.PaymentMethodType,
                persistedPayment.ProviderPayloadMetadata));
        var handler = new InitiateDeliveryPaymentHandler(
            deliveryRepository,
            attemptRepository,
            attemptReadModels,
            paymentRepository,
            providerRepository,
            configurationRepository,
            currencyRepository,
            countryCurrencyRepository,
            stakeholderRepository,
            stakeholderReadModels,
            [providerService],
            consistencyLock,
            commandSender,
            Options.Create(new DeliveryNotificationOptions
            {
                ApplicationBaseUrl = "https://app.pikly.ng/",
                DisplayTimeZoneId = "Africa/Lagos"
            }),
            TimeProvider.System,
            unitOfWork);

        var result = await handler.HandleAsync(
            new(delivery.Id, provider.Id, "request-key", new ActorContext(senderId, tenantId, "correlation", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(InitiateDeliveryPaymentStatus.Success);
        persistedPayment.ShouldNotBeNull();
        persistedPayment.Amount.ShouldBe(delivery.Charge.Amount);
        persistedPayment.CurrencyId.ShouldBe(delivery.Charge.CurrencyId);
        persistedPayment.PaymentIntent.ShouldBe(PaymentIntent.DeliveryPayment);
        persistedAttempt.ShouldNotBeNull();
        persistedAttempt.DeliveryId.ShouldBe(delivery.Id);
        persistedAttempt.PaymentTransactionId.ShouldBe(persistedPayment.Id);
        delivery.PaymentStatus.ShouldBe(DeliveryPaymentStatus.Pending);
        await databaseTransaction.Received(2).CommitAsync(Arg.Any<CancellationToken>());
        await commandSender.Received(1).SendAsync(
            Arg.Is<SendNotificationCommand>(notification =>
                notification.NotificationType == NotificationType.PaymentInitiated &&
                notification.ExpiresAtUtc == null &&
                ((EmailNotificationContent)notification.NotificationContent).Content["ActionUrl"] ==
                    "https://payments.invalid/checkout"),
            Arg.Any<CancellationToken>());
    }

    private static Delivery CreateDelivery(Guid senderId, Guid currencyId) =>
        Delivery.Create(
            senderId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "PKL-INIT-PAYMENT",
            DeliveryType.Instant,
            DeliveryOrderType.PayBeforeDelivery,
            DeliveryAddress.Create("1 Pickup", null, "Lagos", "Lagos", "NG", 6.5m, 3.3m),
            DeliveryAddress.Create("2 Dropoff", null, "Lagos", "Lagos", "NG", 6.6m, 3.4m),
            DeliveryRecipient.Create("Recipient", "+2348012345678"),
            DeliveryCharge.Create(2500m, currencyId),
            [DeliveryItem.Create("Parcel", 1, 1m)],
            new DateTimeOffset(2026, 10, 18, 10, 0, 0, TimeSpan.Zero));
}
