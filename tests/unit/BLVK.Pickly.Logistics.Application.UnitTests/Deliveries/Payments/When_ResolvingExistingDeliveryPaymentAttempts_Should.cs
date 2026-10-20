using BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDeliveryPayments;
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

public sealed class When_ResolvingExistingDeliveryPaymentAttempts_Should
{
    [Fact]
    public void FollowOneBasedEnumConvention()
    {
        ((int)InitiateDeliveryPaymentStatus.Success).ShouldBe(1);
        ((int)InitiateDeliveryPaymentStatus.ProviderConflict).ShouldBe(7);
        ((int)InitiateDeliveryPaymentStatus.PaymentRequiresReview).ShouldBe(8);
        ((int)GetDeliveryPaymentsStatus.Success).ShouldBe(1);
        ((int)GetDeliveryPaymentsStatus.NotAuthenticated).ShouldBe(2);
        ((int)GetDeliveryPaymentsStatus.NotFound).ShouldBe(3);
    }

    [Fact]
    public async Task ReplayTransactionAssociatedWithSameIdempotencyKey()
    {
        var context = new InitiationContext();
        var existing = context.AddAttempt("same-key", PaymentStatus.Failed);

        var result = await context.HandleAsync(context.Provider.Id, "same-key");

        result.Status.ShouldBe(InitiateDeliveryPaymentStatus.PaymentFailed);
        result.PaymentTransactionId.ShouldBe(existing.Id);
        result.PaymentStatus.ShouldBe(PaymentStatus.Failed);
        result.IsReplay.ShouldBeTrue();
        context.AddedTransactions.ShouldBeEmpty();
        await context.CommandSender.DidNotReceive().SendAsync(
            Arg.Any<SendNotificationCommand>(),
            Arg.Any<CancellationToken>());
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

    [Theory]
    [InlineData("unresolved", false)]
    [InlineData("new-request", false)]
    [InlineData("unresolved", true)]
    [InlineData("new-request", true)]
    public async Task RequireReviewForUnresolvedOutcomeRegardlessOfKeyOrProvider(
        string requestKey,
        bool useDifferentProvider)
    {
        var context = new InitiationContext();
        context.AddAttempt("unresolved", PaymentStatus.Unresolved);
        var requestedProviderId = useDifferentProvider
            ? Guid.CreateVersion7()
            : context.Provider.Id;

        var result = await context.HandleAsync(requestedProviderId, requestKey);

        result.Status.ShouldBe(InitiateDeliveryPaymentStatus.PaymentRequiresReview);
        result.PaymentInstruction.ShouldBeNull();
        context.AddedTransactions.ShouldBeEmpty();
        await context.ProviderService.DidNotReceiveWithAnyArgs()
            .InitiatePaymentAsync(default!, default);
    }

    [Fact]
    public async Task KeepIdempotencyKeyBoundToItsOriginalHistoricalAttempt()
    {
        var context = new InitiationContext();
        var failed = context.AddAttempt("KEY-A", PaymentStatus.Failed);
        context.AddAttempt("KEY-B", PaymentStatus.Initiated);

        var result = await context.HandleAsync(context.Provider.Id, "KEY-A");

        result.Status.ShouldBe(InitiateDeliveryPaymentStatus.PaymentFailed);
        result.PaymentTransactionId.ShouldBe(failed.Id);
        await context.ProviderService.DidNotReceiveWithAnyArgs()
            .InitiatePaymentAsync(default!, default);
    }

    [Theory]
    [InlineData(PaymentStatus.Initiated, InitiateDeliveryPaymentStatus.Success)]
    [InlineData(PaymentStatus.Succeeded, InitiateDeliveryPaymentStatus.Success)]
    [InlineData(PaymentStatus.Failed, InitiateDeliveryPaymentStatus.PaymentFailed)]
    [InlineData(PaymentStatus.Expired, InitiateDeliveryPaymentStatus.PaymentExpired)]
    [InlineData(PaymentStatus.Cancelled, InitiateDeliveryPaymentStatus.PaymentCancelled)]
    public async Task ReplayExactAttemptOutcome(
        PaymentStatus paymentStatus,
        InitiateDeliveryPaymentStatus expectedStatus)
    {
        var context = new InitiationContext();
        var existing = context.AddAttempt("exact", paymentStatus);

        var result = await context.HandleAsync(Guid.CreateVersion7(), "exact");

        result.Status.ShouldBe(expectedStatus);
        result.PaymentTransactionId.ShouldBe(existing.Id);
        result.IsReplay.ShouldBeTrue();
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
        await context.CommandSender.DidNotReceive().SendAsync(
            Arg.Any<SendNotificationCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("same-request", "same-request")]
    [InlineData("first-request", "second-request")]
    public async Task SerializeConcurrentRequestsAndCallProviderOnlyOnce(string firstKey, string secondKey)
    {
        var context = new InitiationContext();
        context.EnableSerializedConsistencyLock();

        var results = await Task.WhenAll(
            context.HandleAsync(context.Provider.Id, firstKey),
            context.HandleAsync(context.Provider.Id, secondKey));

        context.AddedTransactions.Count.ShouldBe(1);
        context.AddedAttempts.Count.ShouldBe(1);
        results.Select(result => result.PaymentTransactionId).Distinct().Count().ShouldBe(1);
        await context.ProviderService.Received(1)
            .InitiatePaymentAsync(Arg.Any<PaymentProviderInitiationRequest>(), Arg.Any<CancellationToken>());
        await context.CommandSender.Received(1).SendAsync(
            Arg.Is<SendNotificationCommand>(notification =>
                notification.NotificationType == NotificationType.PaymentInitiated),
            Arg.Any<CancellationToken>());
    }

    private sealed class InitiationContext
    {
        private readonly IRepository<Delivery> _deliveryRepository = Substitute.For<IRepository<Delivery>>();
        private readonly IRepository<DeliveryPaymentAttempt> _attemptRepository = Substitute.For<IRepository<DeliveryPaymentAttempt>>();
        private readonly IDeliveryPaymentAttemptReadModelRepository _attemptReadModels = Substitute.For<IDeliveryPaymentAttemptReadModelRepository>();
        private readonly IRepository<PaymentTransaction> _paymentRepository = Substitute.For<IRepository<PaymentTransaction>>();
        private readonly IRepository<PaymentProvider> _providerRepository = Substitute.For<IRepository<PaymentProvider>>();
        private readonly IRepository<PaymentProviderConfiguration> _configurationRepository = Substitute.For<IRepository<PaymentProviderConfiguration>>();
        private readonly IRepository<Currency> _currencyRepository = Substitute.For<IRepository<Currency>>();
        private readonly IRepository<CountryCurrency> _countryCurrencyRepository = Substitute.For<IRepository<CountryCurrency>>();
        private readonly IRepository<Stakeholder> _stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        private readonly IStakeholderReadModelRepository _stakeholderReadModels = Substitute.For<IStakeholderReadModelRepository>();
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
                    new Dictionary<string, string>
                    {
                        ["paymentLink"] = "https://payments.invalid/checkout"
                    }));

            _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(_databaseTransaction);
            _deliveryRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Delivery>>(), Arg.Any<CancellationToken>()).Returns(Delivery);
            _deliveryRepository.GetByIdAsync(Delivery.Id, Arg.Any<CancellationToken>()).Returns(Delivery);
            _attemptReadModels.ListForDecisionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(_ => Attempts.Select(Project).ToArray());
            _attemptReadModels.GetForDecisionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var transactionId = call.ArgAt<Guid>(0);
                    var attempt = Attempts.SingleOrDefault(candidate => candidate.PaymentTransactionId == transactionId);
                    return attempt is null ? null : Project(attempt);
                });
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
            _stakeholderReadModels.GetByStakeholderIdAsync(_senderId, Arg.Any<CancellationToken>())
                .Returns(new StakeholderReadModel(
                    _senderId,
                    stakeholder.AppUserId,
                    "sender@example.com",
                    _tenantId,
                    countryId,
                    stakeholder.StakeholderTypeId,
                    stakeholder.FirstName,
                    stakeholder.LastName,
                    null,
                    true));
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
                _attemptReadModels,
                _paymentRepository,
                _providerRepository,
                _configurationRepository,
                _currencyRepository,
                _countryCurrencyRepository,
                _stakeholderRepository,
                _stakeholderReadModels,
                [ProviderService],
                _consistencyLock,
                CommandSender,
                Options.Create(new DeliveryNotificationOptions
                {
                    ApplicationBaseUrl = "https://app.pikly.org/",
                    DisplayTimeZoneId = "Africa/Lagos"
                }),
                TimeProvider.System,
                _unitOfWork);
        }

        public Delivery Delivery { get; }
        public Currency Currency { get; }
        public PaymentProvider Provider { get; }
        public IPaymentProviderService ProviderService { get; }
        public ICommandSender CommandSender { get; } = Substitute.For<ICommandSender>();
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
                case PaymentStatus.Succeeded:
                    transaction.MarkSucceeded("provider-reference", "succeeded", DateTimeOffset.UtcNow);
                    break;
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
                case PaymentStatus.Cancelled:
                    typeof(PaymentTransaction).GetProperty(nameof(PaymentTransaction.PaymentStatus))!
                        .SetValue(transaction, PaymentStatus.Cancelled);
                    break;
            }

            Transactions.Add(transaction);
            Attempts.Add(DeliveryPaymentAttempt.Create(Delivery.Id, transaction.Id, key));
            return transaction;
        }

        private DeliveryPaymentAttemptReadModel Project(DeliveryPaymentAttempt attempt)
        {
            var transaction = Transactions.Single(candidate => candidate.Id == attempt.PaymentTransactionId);
            return new(
                attempt.Id,
                attempt.DeliveryId,
                transaction.Id,
                attempt.IdempotencyKey,
                attempt.AppliedAtUtc,
                attempt.ApplicationException,
                transaction.PaymentProviderId,
                Provider.ProviderName,
                Provider.ProviderKey,
                transaction.MerchantReference,
                transaction.PaymentStatus,
                transaction.Amount,
                transaction.CurrencyId,
                Currency.CurrencyCode,
                transaction.VerifiedAmount,
                transaction.VerifiedCurrencyId,
                transaction.VerifiedCurrencyCode,
                transaction.FailureReason,
                transaction.CreatedAtUtc,
                transaction.ExpiresAtUtc,
                transaction.PaymentMethodType,
                transaction.ProviderPayloadMetadata);
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
