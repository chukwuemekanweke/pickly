using BLVK.Pickly.Logistics.Application.Stakeholders.Features.GetProfile;
using BLVK.Pickly.Logistics.Contracts.Commands.Storage;
using BLVK.Pickly.Logistics.Domain.Common.FileUploads.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Stakeholders.Profiles.AvatarUploads.Complete;

[Collection(nameof(ContainersCollection))]
public sealed class When_CompletingAvatarUpload_WithValidQuarantineObject_Should(ContainersFixture fixture)
    : AvatarUploadIntegrationTestBase(fixture)
{
    [Fact]
    public async Task PromoteAvatarAndExposeItFromProfile()
    {
        using var createResponse = await Client.PostAsJsonAsync(
            $"{EndpointUrl.Stakeholders.V1}/me/profile/avatar/uploads",
            new CreateAvatarUploadRequest("avatar.png", "image/png", 12));
        var createPayload = await createResponse.Content.ReadFromJsonAsync<CreateAvatarUploadResponse>();
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        createPayload.ShouldNotBeNull();
        TrackUpload(createPayload.UploadId);

        FileUploadSession upload;
        using (var scope = CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<FileUploadSession>>();
            upload = await repository.GetByIdAsync(createPayload.UploadId)
                ?? throw new InvalidOperationException("The avatar upload was not persisted.");
        }

        var signature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };
        const string etag = "\"integration-etag\"";
        ConfigureStorage(upload, signature, etag);
        using (var uploadClient = new HttpClient())
        using (var content = new ByteArrayContent(signature))
        {
            content.Headers.ContentType = new MediaTypeHeaderValue(createPayload.Headers["Content-Type"]);
            using var uploadResponse = await uploadClient.PutAsync(createPayload.UploadUrl, content);
            uploadResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        using var completeResponse = await Client.PostAsync(
            $"{EndpointUrl.Stakeholders.V1}/me/profile/avatar/uploads/{createPayload.UploadId}/complete",
            null);
        var completePayload = await completeResponse.Content.ReadFromJsonAsync<CompleteAvatarUploadResponse>();
        completeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        completePayload.ShouldNotBeNull();

        using var profileResponse = await Client.GetAsync($"{EndpointUrl.Stakeholders.V1}/me/profile");
        var profile = await profileResponse.Content.ReadFromJsonAsync<GetProfileResponse>();
        profileResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        profile.ShouldNotBeNull();
        profile.AvatarUrl.ShouldBe(completePayload.AvatarUrl);
        profile.AvatarUrl.ShouldNotBeNull();
        profile.AvatarUrl.ShouldContain($"/avatar/{createPayload.UploadId:N}.png");

        using var assertionScope = CreateScope();
        var dbContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var outboxMessage = await dbContext.OutboxMessages.SingleAsync(message =>
            message.Kind == OutboxMessageKind.Command &&
            message.Type == typeof(DeleteQuarantinedObject).FullName &&
            message.Payload.Contains(createPayload.UploadId.ToString()));
        var cleanupCommand = JsonSerializer.Deserialize<DeleteQuarantinedObject>(
            outboxMessage.Payload,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        cleanupCommand.ShouldNotBeNull();
        cleanupCommand.ObjectKey.ShouldBe(upload.QuarantineObjectKey);
        cleanupCommand.StakeholderId.ShouldBe(StakeholderId);
        cleanupCommand.TenantId.ShouldBe(TenantId);
    }

    private void ConfigureStorage(FileUploadSession upload, byte[] content, string etag)
    {
        StorageServer
            .Given(Request.Create()
                .WithPath(BuildPrivatePath(upload.QuarantineObjectKey))
                .WithHeader("Content-Type", "image/png")
                .UsingPut())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK));
        StorageServer
            .Given(Request.Create()
                .WithPath(BuildPrivatePath(upload.QuarantineObjectKey))
                .UsingHead())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Length", content.Length.ToString())
                .WithHeader("Content-Type", "image/png")
                .WithHeader("ETag", etag));
        StorageServer
            .Given(Request.Create()
                .WithPath(BuildPrivatePath(upload.QuarantineObjectKey))
                .WithHeader("If-Match", etag)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.PartialContent)
                .WithHeader("Content-Type", "image/png")
                .WithHeader("ETag", etag)
                .WithBody(content));
        StorageServer
            .Given(Request.Create()
                .WithPath(BuildPublicPath(upload.FinalObjectKey))
                .WithHeader("x-amz-copy-source-if-match", etag)
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/xml")
                .WithBody($"<CopyObjectResult><ETag>{etag}</ETag><LastModified>2026-08-16T12:00:00Z</LastModified></CopyObjectResult>"));
    }
}
