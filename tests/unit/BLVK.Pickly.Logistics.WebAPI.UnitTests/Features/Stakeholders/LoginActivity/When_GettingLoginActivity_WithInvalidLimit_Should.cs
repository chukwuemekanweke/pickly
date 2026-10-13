using BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.LoginActivity;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Stakeholders.LoginActivity;

public sealed class When_GettingLoginActivity_WithInvalidLimit_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var context = new LoginActivityControllerTestContext();

        var result = await context.CreateController().Handle(
            new GetLoginActivityRequest(0),
            CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
