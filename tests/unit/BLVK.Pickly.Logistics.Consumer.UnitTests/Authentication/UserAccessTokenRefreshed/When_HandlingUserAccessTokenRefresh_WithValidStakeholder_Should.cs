using BLVK.Pickly.Logistics.Consumer.Authentication;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Authentication;

public sealed class When_HandlingUserAccessTokenRefresh_WithValidStakeholder_Should
{
    [Fact]
    public async Task PersistLoginActivity()
    {
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var loginActivityIpAddressResolver = Substitute.For<ILoginActivityIpAddressResolver>();
        var loginActivityRepository = Substitute.For<IRepository<LoginActivity>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var messageInboxRepository = Substitute.For<IRepository<MessageInbox>>();
        var userAgentParserService = Substitute.For<IUserAgentParserService>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 4, 21, 12, 0, 0, TimeSpan.Zero));
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var message = new UserAccessTokenRefreshed(ConsumerTestData.IpAddress(), ConsumerTestData.UserAgent())
        {
            StakeholderId = stakeholderId,
            TenantId = tenantId
        };

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(
                stakeholderId,
                Guid.CreateVersion7(),
                ConsumerTestData.Email(),
                tenantId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                ConsumerTestData.FirstName(),
                ConsumerTestData.LastName(),
                null,
                true));
        loginActivityIpAddressResolver.ResolveAsync(message.IpAddress, Arg.Any<CancellationToken>())
            .Returns(new LoginActivityIpAddressResolution(Guid.CreateVersion7(), Guid.CreateVersion7()));
        userAgentParserService.Parse(message.UserAgent)
            .Returns(new UserAgentInfo("Phone", "Android", "Chrome"));

        var sut = new UserAccessTokenRefreshedHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            stakeholderReadModelRepository,
            loginActivityIpAddressResolver,
            loginActivityRepository,
            unitOfWork,
            userAgentParserService,
            timeProvider,
            messageInboxRepository,
            Substitute.For<ILogger<UserAccessTokenRefreshedHandler>>());

        await sut.HandleAsync(message, CancellationToken.None);

        await loginActivityRepository.Received(1).AddAsync(
            Arg.Is<LoginActivity>(activity =>
                activity.StakeholderId == stakeholderId &&
                activity.TenantId == tenantId &&
                activity.ActivityType == LoginActivityType.TokenRefresh),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
