using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.LoginActivity;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Stakeholders.LoginActivity;

public sealed class When_GettingLoginActivity_WithValidRequest_Should
{
    [Fact]
    public async Task ReturnOkUsingCurrentActorScope()
    {
        var context = new LoginActivityControllerTestContext();
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        LoginActivityHistoryCursorRequest? capturedRequest = null;
        context.CurrentActor.ActorId.Returns(stakeholderId.ToString());
        context.CurrentActor.TenantId.Returns(tenantId);
        context.CurrentActor.CorrelationId.Returns("correlation-id");
        context.CurrentActor.FlowId.Returns("flow-id");
        context.Repository.GetByStakeholderAsync(
                Arg.Do<LoginActivityHistoryCursorRequest>(request => capturedRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new LoginActivityHistoryCursorPage(
                [new LoginActivityHistoryReadModel(
                    Guid.CreateVersion7(), LoginActivityType.InitialLogin, DateTimeOffset.UtcNow,
                    "203.0.113.10", "Desktop", "Windows", "Chrome", "Lagos", "Lagos", "Nigeria")],
                false));

        var result = await context.CreateController().Handle(
            new GetLoginActivityRequest(),
            CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<LoginActivityHistoryResponse>();
        response.Activities.Count.ShouldBe(1);
        response.Activities[0].IpAddress.ShouldBe("203.0.113.10");
        capturedRequest.ShouldNotBeNull();
        capturedRequest.StakeholderId.ShouldBe(stakeholderId);
        capturedRequest.TenantId.ShouldBe(tenantId);
    }
}
