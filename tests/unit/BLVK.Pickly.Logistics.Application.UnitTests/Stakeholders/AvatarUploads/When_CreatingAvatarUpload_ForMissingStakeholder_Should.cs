using BLVK.Pickly.Logistics.Application.Stakeholders.Features.CreateAvatarUpload;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Stakeholders.AvatarUploads;

public sealed class When_CreatingAvatarUpload_ForMissingStakeholder_Should
{
    [Fact]
    public async Task ReturnStakeholderNotFound()
    {
        var context = new AvatarUploadHandlerTestContext();
        context.StakeholderRepository.GetByIdAsync(context.StakeholderId, Arg.Any<CancellationToken>())
            .Returns((Stakeholder?)null);

        var result = await context.CreateHandler().HandleAsync(
            new CreateAvatarUploadCommand("avatar.png", "image/png", 12, context.ActorContext()),
            CancellationToken.None);

        result.Status.ShouldBe(CreateAvatarUploadStatus.StakeholderNotFound);
        await context.FileUploadSessionRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
