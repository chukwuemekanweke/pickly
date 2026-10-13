using BLVK.Pickly.Logistics.Application.Stakeholders.Features.CreateAvatarUpload;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Stakeholders.AvatarUploads;

public sealed class When_CreatingAvatarUpload_WithoutAuthenticatedActor_Should
{
    [Fact]
    public async Task ReturnNotAuthenticated()
    {
        var context = new AvatarUploadHandlerTestContext();

        var result = await context.CreateHandler().HandleAsync(
            new CreateAvatarUploadCommand("avatar.png", "image/png", 12, new ActorContext(null, context.TenantId, "c", "f")),
            CancellationToken.None);

        result.Status.ShouldBe(CreateAvatarUploadStatus.NotAuthenticated);
    }
}
