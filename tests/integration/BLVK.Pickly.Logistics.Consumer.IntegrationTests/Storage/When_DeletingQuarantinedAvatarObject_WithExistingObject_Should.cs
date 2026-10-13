using BLVK.Pickly.Logistics.Consumer.IntegrationTests.Infrastructure;
using BLVK.Pickly.Logistics.Consumer.Storage;
using BLVK.Pickly.Logistics.Contracts.Commands.Storage;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Providers.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace BLVK.Pickly.Logistics.Consumer.IntegrationTests.Storage;

[Collection(nameof(ContainersCollection))]
public sealed class When_DeletingQuarantinedAvatarObject_WithExistingObject_Should(ContainersFixture fixture)
    : ConsumerWorkerIntegrationTestBase(fixture)
{
    private const string ApplicationFolder = "integration-tests";
    private const string PrivateBucketName = "private-avatars";
    private readonly WireMockServer _storageServer = WireMockServer.Start();
    private Guid _providerId;
    private Guid _messageId;

    protected override IReadOnlyDictionary<string, string?> GetConfigurationOverrides() =>
        new Dictionary<string, string?>
        {
            ["ObjectStorage:CloudflareR2:Endpoint"] = _storageServer.Urls.Single(),
            ["ObjectStorage:CloudflareR2:ApplicationFolder"] = ApplicationFolder,
            ["ObjectStorage:CloudflareR2:PublicBucketName"] = "public-avatars",
            ["ObjectStorage:CloudflareR2:PrivateBucketName"] = PrivateBucketName,
            ["ObjectStorage:CloudflareR2:AccessKeyId"] = "integration-access-key",
            ["ObjectStorage:CloudflareR2:SecretAccessKey"] = "integration-secret-key",
            ["ObjectStorage:CloudflareR2:PublicBaseUrl"] = "https://cdn.integration.invalid"
        };

    protected override async Task InitializeWorkerTestAsync()
    {
        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<Provider>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var provider = Provider.Create(
            ProviderType.FileStorage,
            "Cloudflare R2",
            ObjectStorageProviderKeys.CloudflareR2,
            true);
        _providerId = provider.Id;
        await repository.AddAsync(provider);
        await unitOfWork.SaveChangesAsync();
    }

    protected override async Task DisposeWorkerTestAsync()
    {
        using var scope = CreateScope();
        var messageRepository = scope.ServiceProvider.GetRequiredService<IRepository<MessageInbox>>();
        var providerRepository = scope.ServiceProvider.GetRequiredService<IRepository<Provider>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var inbox = await messageRepository.FirstOrDefaultAsync(new InboxByMessageIdSpecification(_messageId));
        if (inbox is not null)
        {
            messageRepository.Remove(inbox);
        }

        var provider = await providerRepository.GetByIdAsync(_providerId);
        if (provider is not null)
        {
            providerRepository.Remove(provider);
        }

        await unitOfWork.SaveChangesAsync();
        _storageServer.Dispose();
    }

    [Fact]
    public async Task DeletePrivateObjectAndRecordInboxMessage()
    {
        var uploadId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var objectKey = $"quarantine/avatars/tenants/{tenantId}/{uploadId:N}.png";
        var command = new DeleteQuarantinedObject(uploadId, objectKey)
        {
            MessageId = Guid.CreateVersion7(),
            StakeholderId = Guid.CreateVersion7(),
            TenantId = tenantId
        };
        _messageId = command.MessageId;
        _storageServer
            .Given(Request.Create()
                .WithPath($"/{PrivateBucketName}/{ApplicationFolder}/{objectKey}")
                .UsingDelete())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.NoContent));

        using var scope = CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<DeleteQuarantinedObjectHandler>();
        await handler.HandleAsync(command, CancellationToken.None);

        _storageServer.LogEntries.Count(entry =>
            entry.RequestMessage?.Method == "DELETE" &&
            entry.RequestMessage.Path == $"/{PrivateBucketName}/{ApplicationFolder}/{objectKey}").ShouldBe(1);
        var inbox = await scope.ServiceProvider
            .GetRequiredService<IRepository<MessageInbox>>()
            .FirstOrDefaultAsync(new InboxByMessageIdSpecification(command.MessageId));
        inbox.ShouldNotBeNull();
    }

    private sealed class InboxByMessageIdSpecification : Specification<MessageInbox>
    {
        public InboxByMessageIdSpecification(Guid messageId) =>
            Where(message => message.MessageId == messageId);
    }
}
