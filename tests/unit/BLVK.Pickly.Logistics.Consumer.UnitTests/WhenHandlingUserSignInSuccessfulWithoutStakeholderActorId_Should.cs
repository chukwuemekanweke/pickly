using BLVK.Pickly.Logistics.Consumer.Authentication;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using Shouldly;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests;

public sealed class WhenHandlingUserSignInSuccessfulWithoutStakeholderActorId_Should
{
    [Fact]
    public async Task ThrowNonTransientException()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var loginActivityIpAddressResolver = Substitute.For<ILoginActivityIpAddressResolver>();
        var loginActivityRepository = Substitute.For<IRepository<LoginActivity>>();
        var userAgentParserService = Substitute.For<IUserAgentParserService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var messageInboxRepository = Substitute.For<IRepository<MessageInbox>>();
        var ipAddress = ConsumerTestData.IpAddress();
        var userAgent = ConsumerTestData.UserAgent();
        var tenantId = Guid.CreateVersion7();

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));

        var action = async () => await new UserSignInSuccessfulHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            identityService,
            stakeholderReadModelRepository,
            commandSender,
            loginActivityIpAddressResolver,
            loginActivityRepository,
            unitOfWork,
            userAgentParserService,
            TimeProvider.System,
            messageInboxRepository,
            Substitute.For<ILogger<UserSignInSuccessfulHandler>>()).HandleAsync(
                new UserSignInSuccessful(ipAddress, userAgent)
                {
                    TenantId = tenantId
                },
                CancellationToken.None);

        await action.ShouldThrowAsync<CannotProcessMessageNonTransientException>();
        await identityService.DidNotReceive().FindByIdAsync(Arg.Any<Guid>());
    }
}

