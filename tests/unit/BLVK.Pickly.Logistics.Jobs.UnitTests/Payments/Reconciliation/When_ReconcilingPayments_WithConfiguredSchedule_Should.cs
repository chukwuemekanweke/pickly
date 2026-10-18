using BLVK.Pickly.Logistics.Application;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.Jobs.Infrastructure.BackgroundServices;
using BLVK.Pickly.Logistics.Jobs.Payments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BLVK.Pickly.Logistics.Jobs.UnitTests.Payments.Reconciliation;

public sealed class When_ReconcilingPayments_WithConfiguredSchedule_Should
{
    [Fact]
    public async Task InvokeReconciliationService()
    {
        var paymentTransactionRepository = Substitute.For<IRepository<PaymentTransaction>>();
        var currencyRepository = Substitute.For<IRepository<Currency>>();
        var paymentProviderRepository = Substitute.For<IRepository<PaymentProvider>>();
        var deliveryRepository = Substitute.For<IRepository<Delivery>>();
        var deliveryPaymentAttemptRepository = Substitute.For<IRepository<DeliveryPaymentAttempt>>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var paymentProviderService = Substitute.For<IPaymentProviderService>();
        var state = new BackgroundServiceReadinessState([new BackgroundServiceDescriptor(PaymentReconciliationProcessor.ServiceName)]);
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 4, 25, 15, 0, 0, TimeSpan.Zero));
        var services = new ServiceCollection()
            .AddSingleton(paymentTransactionRepository)
            .AddSingleton(currencyRepository)
            .AddSingleton(paymentProviderRepository)
            .AddSingleton(deliveryRepository)
            .AddSingleton(deliveryPaymentAttemptRepository)
            .AddSingleton<IEnumerable<IPaymentProviderService>>([paymentProviderService])
            .AddSingleton(eventPublisher)
            .AddSingleton(customTelemetryContext)
            .AddSingleton(unitOfWork)
            .AddSingleton<TimeProvider>(clock)
            .AddApplication()
            .BuildServiceProvider();

        var listAsyncCalled = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        paymentTransactionRepository.When(repo => repo.ListAsync(Arg.Any<ISpecification<PaymentTransaction>>(), Arg.Any<CancellationToken>()))
            .Do(_ => listAsyncCalled.TrySetResult(null));

        paymentTransactionRepository.ListAsync(Arg.Any<ISpecification<PaymentTransaction>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = new PaymentReconciliationProcessor(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new PaymentReconciliationOptions
            {
                BatchSize = 10,
                PollInterval = TimeSpan.FromSeconds(1),
                MaxInitiatedAge = TimeSpan.FromHours(48),
                StaleThreshold = TimeSpan.FromMinutes(5),
                RecheckDelay = TimeSpan.FromMinutes(2)
            }),
            state,
            clock,
            NullLogger<PaymentReconciliationProcessor>.Instance);

        await sut.StartAsync(CancellationToken.None);
        await WaitForConditionAsync(() => state.IsReady);
        await listAsyncCalled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await sut.StopAsync(CancellationToken.None);

        state.IsReady.ShouldBeTrue();
        await paymentTransactionRepository.Received(1).ListAsync(Arg.Any<ISpecification<PaymentTransaction>>(), Arg.Any<CancellationToken>());
    }

    private static async Task WaitForConditionAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new InvalidOperationException("Condition not met in time.");
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
