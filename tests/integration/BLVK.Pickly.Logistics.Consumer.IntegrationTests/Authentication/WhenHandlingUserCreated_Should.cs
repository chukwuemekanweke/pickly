using BLVK.Pickly.Logistics.Consumer.IntegrationTests.Infrastructure;
using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Chidelu.Integration.Messaging.RabbitMQ.Core;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Consumer.IntegrationTests.Authentication;

[Collection(nameof(ContainersCollection))]
public sealed class WhenHandlingUserCreated_Should(ContainersFixture fixture)
    : ConsumerWorkerIntegrationTestBase(fixture)
{
    private const string Password = "P@ssw0rd123!";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ContainersFixture _fixture = fixture;
    private string _email = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private Guid _tenantId;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private Guid _userId;

    protected override Task InitializeWorkerTestAsync() => CreatePendingUserAsync();

    protected override Task DisposeWorkerTestAsync() => DeleteAuthenticationRecordsAsync();

    [Fact]
    public async Task GenerateSignUpOtpAndQueueNotificationCommand()
    {
        await WhenPublishingUserCreated();
        await ThenTheSignUpOtpNotificationCommandIsQueued();

        async Task WhenPublishingUserCreated()
        {
            var requestedAtUtc = DateTimeOffset.UtcNow;
            var publisherConfig = new PublisherConfig
            {
                ServiceName = "BLVK.Pickly.Logistics.Consumer.IntegrationTests.Publisher",
                HostName = _fixture.RabbitMqHostName,
                Port = _fixture.RabbitMqPort,
                UserName = _fixture.RabbitMqUserName,
                Password = _fixture.RabbitMqPassword,
                VirtualHost = _fixture.RabbitMqVirtualHost,
                EventsExchange = "x.events.blvkpicklylogistics.integrationtests"
            };

            await using var publisherServices = new ServiceCollection()
                .AddLogging()
                .AddPublisher(publisherConfig)
                .BuildServiceProvider();

            var publisher = publisherServices.GetRequiredKeyedService<IPublisher>(publisherConfig.Key);
            await publisher.PublishAsync(
                new UserCreated
                {
                    StakeholderId = _stakeholderId,
                    TenantId = _tenantId,
                    RequestedAtUtc = requestedAtUtc,
                    ExpiresAtUtc = requestedAtUtc.Add(AuthenticationOtpDefaults.EmailConfirmationLifetime)
                },
                CancellationToken.None,
                new Dictionary<string, string>
                {
                    [KnownMetadata.CorrelationId] = Guid.CreateVersion7().ToString("N")
                });
        }

        async Task ThenTheSignUpOtpNotificationCommandIsQueued()
        {
            await WaitForConditionAsync(async () =>
            {
                using var scope = CreateDbContextScope();
                var command = await scope.DbContext.OutboxMessages
                    .Where(message =>
                        message.Kind == OutboxMessageKind.Command &&
                        message.Type == typeof(SendNotificationCommand).FullName! &&
                        message.Payload.Contains(_email))
                    .OrderByDescending(message => message.EnqueuedAtUtc)
                    .FirstOrDefaultAsync();

                return command is not null;
            });

            using var assertionScope = CreateDbContextScope();
            var outboxMessage = await assertionScope.DbContext.OutboxMessages
                .Where(message =>
                    message.Kind == OutboxMessageKind.Command &&
                    message.Type == typeof(SendNotificationCommand).FullName! &&
                    message.Payload.Contains(_email))
                .OrderByDescending(message => message.EnqueuedAtUtc)
                .FirstAsync();

            var command = JsonSerializer.Deserialize<SendNotificationCommand>(outboxMessage.Payload, SerializerOptions);

            command.ShouldNotBeNull();
            command.TenantId.ShouldBe(_tenantId);
            command.CountryId.ShouldBe(_countryId);
            command.StakeholderId.ShouldBe(_stakeholderId);
            command.NotificationType.ShouldBe(NotificationType.EmailConfirmationOtp);
            command.NotificationMedium.ShouldBe(NotificationMedium.Email);
            command.NotificationContent.ShouldBeOfType<EmailNotificationContent>();

            var content = (EmailNotificationContent)command.NotificationContent;
            content.To.ShouldBe(_email);
            content.Content["FirstName"].ShouldBe(_firstName);
            content.Content["LastName"].ShouldBe(_lastName);
            content.Content["OtpCode"].ShouldNotBeNullOrWhiteSpace();
            content.Content["OtpExpiresAtUtc"].ShouldNotBeNullOrWhiteSpace();
            content.Content["Product"].ShouldBe("BLVK.Pickly.Logistics");

            using var scope = CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
            var twoFactorOtpService = scope.ServiceProvider.GetRequiredService<ITwoFactorOtpService>();
            var user = await repository.GetByEmailAsync(_email);
            user.ShouldNotBeNull();
            (await twoFactorOtpService.ValidateOtpAsync(
                user.Id,
                content.Content["OtpCode"],
                OtpIntent.EmailConfirmation,
                CancellationToken.None)).ShouldBeTrue();
        }
    }

    private async Task CreatePendingUserAsync()
    {
        _email = ConsumerIntegrationTestData.Email();
        _firstName = ConsumerIntegrationTestData.FirstName();
        _lastName = ConsumerIntegrationTestData.LastName();

        _stakeholderId = Guid.CreateVersion7();
        _tenantId = Guid.CreateVersion7();
        _countryId = Guid.CreateVersion7();

        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = AppUser.Create(_email, _firstName, _lastName);

        var createResult = await identityService.CreateAsync(user);
        createResult.Succeeded.ShouldBeTrue();
        _userId = user.Id;

        var now = timeProvider.GetUtcNow();
        var stakeholderType = StakeholderType.Create(_tenantId, "Individual Customer", "individual-customer");
        var stakeholder = Stakeholder.Create(user.Id, _tenantId, _countryId, stakeholderType.Id, _firstName, _lastName);

        await dbContext.StakeholderTypes.AddAsync(stakeholderType);
        await dbContext.Stakeholders.AddAsync(stakeholder);
        await dbContext.SaveChangesAsync();

        _stakeholderId = stakeholder.Id;
        _stakeholderTypeId = stakeholderType.Id;
    }

    private async Task DeleteAuthenticationRecordsAsync()
    {
        if (string.IsNullOrWhiteSpace(_email))
        {
            return;
        }

        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await repository.GetByEmailAsync(_email);

        var outboxMessages = await dbContext.OutboxMessages
            .Where(message =>
                message.Kind == OutboxMessageKind.Command &&
                message.Type == typeof(SendNotificationCommand).FullName! &&
                message.Payload.Contains(_email))
            .ToListAsync();
        if (outboxMessages.Count > 0)
        {
            dbContext.OutboxMessages.RemoveRange(outboxMessages);
        }

        var stakeholders = await dbContext.Stakeholders
            .Where(stakeholder => stakeholder.Id == _stakeholderId)
            .ToListAsync();
        if (stakeholders.Count > 0)
        {
            dbContext.Stakeholders.RemoveRange(stakeholders);
        }

        var stakeholderTypes = await dbContext.StakeholderTypes
            .Where(stakeholderType => stakeholderType.Id == _stakeholderTypeId)
            .ToListAsync();
        if (stakeholderTypes.Count > 0)
        {
            dbContext.StakeholderTypes.RemoveRange(stakeholderTypes);
        }

        if (user is null)
        {
            await dbContext.SaveChangesAsync();
            return;
        }

        repository.Remove(user);
        await unitOfWork.SaveChangesAsync();
    }
}








