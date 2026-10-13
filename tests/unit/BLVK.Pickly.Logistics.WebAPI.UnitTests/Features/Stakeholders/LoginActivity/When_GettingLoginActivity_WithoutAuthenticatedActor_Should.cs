using BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.LoginActivity;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Stakeholders.LoginActivity;

public sealed class When_GettingLoginActivity_WithoutAuthenticatedActor_Should
{
    [Fact]
    public async Task ReturnUnauthorizedWithoutQueryingRepository()
    {
        var context = new LoginActivityControllerTestContext();
        context.CurrentActor.TenantId.Returns(Guid.CreateVersion7());

        var result = await context.CreateController().Handle(
            new GetLoginActivityRequest(),
            CancellationToken.None);

        result.Result.ShouldBeOfType<UnauthorizedResult>();
        await context.Repository.DidNotReceiveWithAnyArgs()
            .GetByStakeholderAsync(default!, default);
    }
}
