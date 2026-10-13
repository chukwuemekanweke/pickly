using BLVK.Pickly.Logistics.Domain.Common.FileUploads.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Stakeholders.Profiles.AvatarUploads.Create;

[Collection(nameof(ContainersCollection))]
public sealed class When_CreatingAvatarUpload_WithAuthenticatedStakeholder_Should(ContainersFixture fixture)
    : AvatarUploadIntegrationTestBase(fixture)
{
    [Fact]
    public async Task ReturnPrivateUploadInstructionsAndPersistPendingUpload()
    {
        using var response = await Client.PostAsJsonAsync(
            $"{EndpointUrl.Stakeholders.V1}/me/profile/avatar/uploads",
            new CreateAvatarUploadRequest("profile.exe", "image/jpeg", 128));
        var payload = await response.Content.ReadFromJsonAsync<CreateAvatarUploadResponse>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.ShouldNotBeNull();
        payload.Method.ShouldBe("PUT");
        payload.Headers["Content-Type"].ShouldBe("image/jpeg");
        payload.UploadUrl.ShouldStartWith(StorageServer.Urls.Single());
        payload.UploadUrl.ShouldContain($"/{PrivateBucketName}/{ApplicationFolder}/quarantine/avatars/");
        TrackUpload(payload.UploadId);
        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<FileUploadSession>>();
        var upload = await repository.GetByIdAsync(payload.UploadId);
        upload.ShouldNotBeNull();
        upload.Status.ShouldBe(FileUploadStatus.Pending);
        upload.OwnerType.ShouldBe("stakeholder");
        upload.OwnerId.ShouldBe(StakeholderId);
        upload.InitiatedByStakeholderId.ShouldBe(StakeholderId);
        upload.Purpose.ShouldBe("stakeholder-profile-avatar");
        upload.PolicyKey.ShouldBe("stakeholder-avatar-v1");
        upload.DestinationVisibility.ShouldBe(ObjectStorageVisibility.Public);
        upload.FileExtension.ShouldBe(".jpg");
        upload.QuarantineObjectKey.ShouldNotContain("profile.exe");
    }
}
