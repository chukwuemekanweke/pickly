using BLVK.Pickly.Logistics.Application.Deliveries.Features.InitiateDeliveryPayment;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
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
        var paymentRepository = Substitute.For<IRepository<PaymentTransaction>>();
        var providerRepository = Substitute.For<IRepository<PaymentProvider>>();
        var configurationRepository = Substitute.For<IRepository<PaymentProviderConfiguration>>();
        var currencyRepository = Substitute.For<IRepository<Currency>>();
        var countryCurrencyRepository = Substitute.For<IRepository<CountryCurrency>>();
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var consistencyLock = Substitute.For<IDeliveryPaymentInitiationConsistencyLock>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var databaseTransaction = Substitute.For<IUnitOfWorkTransaction>();
        var providerService = Substitute.For<IPaymentProviderService>();
        unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(databaseTransaction);
        deliveryRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Delivery>>(), Arg.Any<CancellationToken>()).Returns(delivery);
        attemptRepository.ListAsync(Arg.Any<ISpecification<DeliveryPaymentAttempt>>(), Arg.Any<CancellationToken>()).Returns([]);
        stakeholderRepository.GetByIdAsync(senderId, Arg.Any<CancellationToken>()).Returns(stakeholder);
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
        attemptRepository.When(repository => repository.AddAsync(Arg.Any<DeliveryPaymentAttempt>(), Arg.Any<CancellationToken>()))
            .Do(call => persistedAttempt = call.ArgAt<DeliveryPaymentAttempt>(0));
        var handler = new InitiateDeliveryPaymentHandler(
            deliveryRepository,
            attemptRepository,
            paymentRepository,
            providerRepository,
            configurationRepository,
            currencyRepository,
            countryCurrencyRepository,
            stakeholderRepository,
            [providerService],
            consistencyLock,
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
        delivery.ActivePaymentTransactionId.ShouldBe(persistedPayment.Id);
        await databaseTransaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
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
