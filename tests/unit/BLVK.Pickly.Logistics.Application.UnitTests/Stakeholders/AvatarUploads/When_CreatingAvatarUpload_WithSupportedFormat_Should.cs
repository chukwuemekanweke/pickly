using BLVK.Pickly.Logistics.Application.Stakeholders.Features.CreateAvatarUpload;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Stakeholders.AvatarUploads;

public sealed class When_CreatingAvatarUpload_WithSupportedFormat_Should
{
    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("image/webp", ".webp")]
    public async Task PersistPendingUploadAndReturnSignedPut(string contentType, string extension)
    {
        var context = new AvatarUploadHandlerTestContext();
        FileUploadSession? persistedUpload = null;
        context.FileUploadSessionRepository.AddAsync(Arg.Do<FileUploadSession>(upload => persistedUpload = upload), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        context.ObjectStorageService.CreatePrivatePresignedUploadAsync(
                Arg.Any<ObjectStoragePresignedUploadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call => new ObjectStoragePresignedUploadResult(
                "https://r2.example/upload?signature=secret",
                new Dictionary<string, string> { ["Content-Type"] = contentType },
                call.Arg<ObjectStoragePresignedUploadRequest>().ExpiresAtUtc));

        var result = await context.CreateHandler().HandleAsync(
            new CreateAvatarUploadCommand("untrusted.exe", contentType, 128, context.ActorContext()),
            CancellationToken.None);

        result.Status.ShouldBe(CreateAvatarUploadStatus.Success);
        result.Headers!["Content-Type"].ShouldBe(contentType);
        persistedUpload.ShouldNotBeNull();
        persistedUpload.Status.ShouldBe(FileUploadStatus.Pending);
        persistedUpload.FileExtension.ShouldBe(extension);
        persistedUpload.QuarantineObjectKey.ShouldContain($"/{persistedUpload.Id:N}{extension}");
        persistedUpload.QuarantineObjectKey.ShouldNotContain("untrusted.exe");
        persistedUpload.FinalObjectKey.ShouldContain($"/{persistedUpload.Id:N}{extension}");
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
