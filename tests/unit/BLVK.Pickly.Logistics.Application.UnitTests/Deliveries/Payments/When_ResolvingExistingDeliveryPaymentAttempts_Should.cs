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

public sealed class When_ResolvingExistingDeliveryPaymentAttempts_Should
{
    [Fact]
    public void FollowOneBasedEnumConvention()
    {
        ((int)InitiateDeliveryPaymentStatus.Success).ShouldBe(1);
        ((int)InitiateDeliveryPaymentStatus.ProviderConflict).ShouldBe(7);
    }

    [Fact]
    public async Task ReplayTransactionAssociatedWithSameIdempotencyKey()
    {
        var context = new InitiationContext();
        var existing = context.AddAttempt("same-key", PaymentStatus.Failed);

        var result = await context.HandleAsync(context.Provider.Id, "same-key");

        result.Status.ShouldBe(InitiateDeliveryPaymentStatus.Success);
        result.PaymentTransactionId.ShouldBe(existing.Id);
        result.PaymentStatus.ShouldBe(PaymentStatus.Failed);
        result.IsReplay.ShouldBeTrue();
        context.AddedTransactions.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReuseOutstandingTransactionWithoutAssumingLatestAttemptIsActive()
    {
        var context = new InitiationContext();
        var outstanding = context.AddAttempt("outstanding", PaymentStatus.Initiated);
        context.AddAttempt("newer-failed", PaymentStatus.Failed);

        var result = await context.HandleAsync(context.Provider.Id, "new-request");

        result.PaymentTransactionId.ShouldBe(outstanding.Id);
        result.IsReplay.ShouldBeTrue();
        context.AddedTransactions.ShouldBeEmpty();
    }

    [Fact]
    public async Task RejectDifferentProviderWhileFinancialOutcomeIsOutstanding()
    {
        var context = new InitiationContext();
        context.AddAttempt("unresolved", PaymentStatus.Unresolved);
        var otherProvider = PaymentProvider.Create("Other", "other", true);

        var result = await context.HandleAsync(otherProvider.Id, "new-request");

        result.Status.ShouldBe(InitiateDeliveryPaymentStatus.ProviderConflict);
        context.AddedTransactions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Expired)]
    public async Task CreateNewAttemptAfterCompletedUnsuccessfulAttempt(PaymentStatus completedStatus)
    {
        var context = new InitiationContext();
        context.AddAttempt("old-attempt", completedStatus);

        var result = await context.HandleAsync(context.Provider.Id, "new-attempt");

        result.Status.ShouldBe(InitiateDeliveryPaymentStatus.Success);
        context.AddedTransactions.Count.ShouldBe(1);
        context.AddedAttempts.Count.ShouldBe(1);
        context.Attempts.Count.ShouldBe(2);
        context.AddedAttempts.Single().IdempotencyKey.ShouldBe("new-attempt");
        context.Delivery.PaymentStatus.ShouldBe(DeliveryPaymentStatus.Pending);
    }

    [Fact]
    public async Task RetryPendingInitiationWithStableMerchantReference()
    {
        var context = new InitiationContext();
        var existing = context.AddAttempt("retry", PaymentStatus.PendingInitiation);
        string? initiatedReference = null;
        context.ProviderService.InitiatePaymentAsync(
                Arg.Any<PaymentProviderInitiationRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                initiatedReference = call.ArgAt<PaymentProviderInitiationRequest>(0).MerchantReference;
                return new PaymentProviderInitiationResult(
                    "provider-reference",
                    context.Provider.ProviderKey,
                    PaymentMethodType.PaymentLink,
                    null,
                    new Dictionary<string, string>());
            });

        var result = await context.HandleAsync(context.Provider.Id, "retry");

        result.PaymentTransactionId.ShouldBe(existing.Id);
        initiatedReference.ShouldBe(existing.MerchantReference);
        context.AddedTransactions.ShouldBeEmpty();
    }

    [Fact]
    public async Task SerializeConcurrentRequestsAndReserveOnlyOneTransaction()
    {
        var context = new InitiationContext();
        context.EnableSerializedConsistencyLock();

        var results = await Task.WhenAll(
            context.HandleAsync(context.Provider.Id, "first-request"),
            context.HandleAsync(context.Provider.Id, "second-request"));

        context.AddedTransactions.Count.ShouldBe(1);
        context.AddedAttempts.Count.ShouldBe(1);
        results.Select(result => result.PaymentTransactionId).Distinct().Count().ShouldBe(1);
    }

    private sealed class InitiationContext
    {
        private readonly IRepository<Delivery> _deliveryRepository = Substitute.For<IRepository<Delivery>>();
        private readonly IRepository<DeliveryPaymentAttempt> _attemptRepository = Substitute.For<IRepository<DeliveryPaymentAttempt>>();
        private readonly IRepository<PaymentTransaction> _paymentRepository = Substitute.For<IRepository<PaymentTransaction>>();
        private readonly IRepository<PaymentProvider> _providerRepository = Substitute.For<IRepository<PaymentProvider>>();
        private readonly IRepository<PaymentProviderConfiguration> _configurationRepository = Substitute.For<IRepository<PaymentProviderConfiguration>>();
        private readonly IRepository<Currency> _currencyRepository = Substitute.For<IRepository<Currency>>();
        private readonly IRepository<CountryCurrency> _countryCurrencyRepository = Substitute.For<IRepository<CountryCurrency>>();
        private readonly IRepository<Stakeholder> _stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        private readonly IDeliveryPaymentInitiationConsistencyLock _consistencyLock = Substitute.For<IDeliveryPaymentInitiationConsistencyLock>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly IUnitOfWorkTransaction _databaseTransaction = Substitute.For<IUnitOfWorkTransaction>();
        private readonly InitiateDeliveryPaymentHandler _handler;
        private readonly Guid _senderId = Guid.CreateVersion7();
        private readonly Guid _tenantId = Guid.CreateVersion7();

        public InitiationContext()
        {
            Currency = Currency.Create("NGN", "Naira", true);
            var countryId = Guid.CreateVersion7();
            var stakeholder = Stakeholder.Create(
                Guid.CreateVersion7(), _tenantId, countryId, Guid.CreateVersion7(), "Ada", "Sender");
            typeof(Domain.Common.Entities.Entity).GetProperty(nameof(Domain.Common.Entities.Entity.Id))!
                .SetValue(stakeholder, _senderId);
            Delivery = CreateDelivery(_senderId, Currency.Id);
            Provider = PaymentProvider.Create("Provider", "provider", true);
            Provider.SetConfiguration(Currency.Id, PaymentIntent.DeliveryPayment, PaymentMethodType.PaymentLink, true);
            ProviderService = Substitute.For<IPaymentProviderService>();
            ProviderService.ProviderKey.Returns(Provider.ProviderKey);
            ProviderService.InitiatePaymentAsync(
                    Arg.Any<PaymentProviderInitiationRequest>(), Arg.Any<CancellationToken>())
                .Returns(new PaymentProviderInitiationResult(
                    "provider-reference",
                    Provider.ProviderKey,
                    PaymentMethodType.PaymentLink,
                    null,
                    new Dictionary<string, string>()));

            _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(_databaseTransaction);
            _deliveryRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Delivery>>(), Arg.Any<CancellationToken>()).Returns(Delivery);
            _attemptRepository.ListAsync(Arg.Any<ISpecification<DeliveryPaymentAttempt>>(), Arg.Any<CancellationToken>())
                .Returns(_ => Attempts);
            _paymentRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(call => Transactions.SingleOrDefault(transaction => transaction.Id == call.ArgAt<Guid>(0)));
            _providerRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(call => call.ArgAt<Guid>(0) == Provider.Id ? Provider : null);
            _providerRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentProvider>>(), Arg.Any<CancellationToken>()).Returns(Provider);
            _configurationRepository.FirstOrDefaultAsync(
                    Arg.Any<ISpecification<PaymentProviderConfiguration>>(), Arg.Any<CancellationToken>())
                .Returns(Provider.Configurations.Single());
            _currencyRepository.GetByIdAsync(Currency.Id, Arg.Any<CancellationToken>()).Returns(Currency);
            _currencyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Currency>>(), Arg.Any<CancellationToken>()).Returns(Currency);
            _countryCurrencyRepository.FirstOrDefaultAsync(
                    Arg.Any<ISpecification<CountryCurrency>>(), Arg.Any<CancellationToken>())
                .Returns(CountryCurrency.Create(countryId, Currency.Id, true, true));
            _stakeholderRepository.GetByIdAsync(_senderId, Arg.Any<CancellationToken>()).Returns(stakeholder);
            _paymentRepository.When(repository => repository.AddAsync(Arg.Any<PaymentTransaction>(), Arg.Any<CancellationToken>()))
                .Do(call =>
                {
                    var transaction = call.ArgAt<PaymentTransaction>(0);
                    AddedTransactions.Add(transaction);
                    Transactions.Add(transaction);
                });
            _attemptRepository.When(repository => repository.AddAsync(Arg.Any<DeliveryPaymentAttempt>(), Arg.Any<CancellationToken>()))
                .Do(call =>
                {
                    var attempt = call.ArgAt<DeliveryPaymentAttempt>(0);
                    AddedAttempts.Add(attempt);
                    Attempts.Add(attempt);
                });

            _handler = new InitiateDeliveryPaymentHandler(
                _deliveryRepository,
                _attemptRepository,
                _paymentRepository,
                _providerRepository,
                _configurationRepository,
                _currencyRepository,
                _countryCurrencyRepository,
                _stakeholderRepository,
                [ProviderService],
                _consistencyLock,
                _unitOfWork);
        }

        public Delivery Delivery { get; }
        public Currency Currency { get; }
        public PaymentProvider Provider { get; }
        public IPaymentProviderService ProviderService { get; }
        public List<DeliveryPaymentAttempt> Attempts { get; } = [];
        public List<PaymentTransaction> Transactions { get; } = [];
        public List<PaymentTransaction> AddedTransactions { get; } = [];
        public List<DeliveryPaymentAttempt> AddedAttempts { get; } = [];

        public void EnableSerializedConsistencyLock()
        {
            var gate = new SemaphoreSlim(1, 1);
            _consistencyLock.AcquireAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(call => gate.WaitAsync(call.ArgAt<CancellationToken>(1)));
            _databaseTransaction.DisposeAsync().Returns(_ =>
            {
                gate.Release();
                return ValueTask.CompletedTask;
            });
        }

        public PaymentTransaction AddAttempt(string key, PaymentStatus status)
        {
            var transaction = PaymentTransaction.Create(
                $"delivery_{Guid.CreateVersion7():N}",
                PaymentIntent.DeliveryPayment,
                Provider.Id,
                Delivery.Charge.Amount,
                Currency.Id,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                _senderId,
                _tenantId);
            if (status != PaymentStatus.PendingInitiation)
            {
                transaction.MarkInitiated("provider-reference", null, null, "initiated");
            }

            switch (status)
            {
                case PaymentStatus.Failed:
                    transaction.MarkFailed("provider-reference", "failed", "failed", DateTimeOffset.UtcNow);
                    break;
                case PaymentStatus.Expired:
                    transaction.MarkExpired("provider-reference", "expired", "expired");
                    break;
                case PaymentStatus.Unresolved:
                    transaction.MarkUnresolved(
                        "provider-reference", 100m, null, "USD", DateTimeOffset.UtcNow, "mismatch", "unresolved");
                    break;
            }

            Transactions.Add(transaction);
            Attempts.Add(DeliveryPaymentAttempt.Create(Delivery.Id, transaction.Id, key));
            return transaction;
        }

        public Task<InitiateDeliveryPaymentResult> HandleAsync(Guid providerId, string key) =>
            _handler.HandleAsync(
                new InitiateDeliveryPaymentCommand(
                    Delivery.Id,
                    providerId,
                    key,
                    new ActorContext(_senderId, _tenantId, "correlation", "flow")),
                CancellationToken.None);

        private static Delivery CreateDelivery(Guid senderId, Guid currencyId) =>
            Domain.Deliveries.Entities.Delivery.Create(
                senderId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "PKL-ATTEMPT-TEST",
                DeliveryType.Instant,
                DeliveryOrderType.PayBeforeDelivery,
                DeliveryAddress.Create("1 Pickup", null, "Lagos", "Lagos", "NG", 6.5m, 3.3m),
                DeliveryAddress.Create("2 Dropoff", null, "Lagos", "Lagos", "NG", 6.6m, 3.4m),
                DeliveryRecipient.Create("Recipient", "+2348012345678"),
                DeliveryCharge.Create(5000m, currencyId),
                [DeliveryItem.Create("Parcel", 1, 1m)],
                DateTimeOffset.UtcNow);
    }
}
