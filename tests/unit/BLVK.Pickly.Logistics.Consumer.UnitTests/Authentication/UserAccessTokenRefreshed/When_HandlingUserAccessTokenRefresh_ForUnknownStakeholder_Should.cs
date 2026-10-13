using BLVK.Pickly.Logistics.Consumer.Authentication;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using Shouldly;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Authentication;

public sealed class When_HandlingUserAccessTokenRefresh_ForUnknownStakeholder_Should
{
    [Fact]
    public async Task ThrowNonTransientException()
    {
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderRepository = Substitute.For<IStakeholderReadModelRepository>();
        var stakeholderId = Guid.CreateVersion7();

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        stakeholderRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns((StakeholderReadModel?)null);

        var sut = new UserAccessTokenRefreshedHandler(
            Substitute.For<ICustomTelemetryContext>(),
            Substitute.For<ICurrentActorAccessor>(),
            messageContext,
            stakeholderRepository,
            Substitute.For<ILoginActivityIpAddressResolver>(),
            Substitute.For<IRepository<LoginActivity>>(),
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IUserAgentParserService>(),
            TimeProvider.System,
            Substitute.For<IRepository<MessageInbox>>(),
            Substitute.For<ILogger<UserAccessTokenRefreshedHandler>>());

        var exception = await Should.ThrowAsync<CannotProcessMessageNonTransientException>(() =>
            sut.HandleAsync(
                new UserAccessTokenRefreshed(ConsumerTestData.IpAddress(), ConsumerTestData.UserAgent())
                {
                    StakeholderId = stakeholderId,
                    TenantId = Guid.CreateVersion7()
                },
                CancellationToken.None));

        exception.Message.ShouldContain("no stakeholder could be found", Case.Insensitive);
    }
}
