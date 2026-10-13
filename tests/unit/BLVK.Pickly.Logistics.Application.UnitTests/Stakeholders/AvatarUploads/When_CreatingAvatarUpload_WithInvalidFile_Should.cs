using BLVK.Pickly.Logistics.Application.Stakeholders.Features.CreateAvatarUpload;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Stakeholders.AvatarUploads;

public sealed class When_CreatingAvatarUpload_WithInvalidFile_Should
{
    [Theory]
    [InlineData("avatar.svg", "image/svg+xml", 100)]
    [InlineData("avatar.gif", "image/gif", 100)]
    [InlineData("avatar.bmp", "image/bmp", 100)]
    [InlineData("avatar.png", "image/png", 0)]
    [InlineData("avatar.png", "image/png", 2097153)]
    [InlineData("", "image/png", 100)]
    public async Task ReturnInvalidFile(string fileName, string contentType, long contentLength)
    {
        var context = new AvatarUploadHandlerTestContext();

        var result = await context.CreateHandler().HandleAsync(
            new CreateAvatarUploadCommand(fileName, contentType, contentLength, context.ActorContext()),
            CancellationToken.None);

        result.Status.ShouldBe(CreateAvatarUploadStatus.InvalidFile);
        await context.FileUploadSessionRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
