using BLVK.Pickly.Logistics.Application.Authentication.Features.GetLoginActivityHistory;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.LoginActivity;

public sealed class When_GettingLoginActivityHistory_WithValidActor_Should
{
    [Fact]
    public async Task ReturnMappedHistoryScopedToTenantAndStakeholder()
    {
        var context = new LoginActivityHistoryTestContext();
        var tenantId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var occurredAtUtc = new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);
        LoginActivityHistoryCursorRequest? capturedRequest = null;
        context.Repository.GetByStakeholderAsync(
                Arg.Do<LoginActivityHistoryCursorRequest>(request => capturedRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new LoginActivityHistoryCursorPage(
                [new LoginActivityHistoryReadModel(
                    Guid.CreateVersion7(),
                    LoginActivityType.InitialLogin,
                    occurredAtUtc,
                    "203.0.113.10",
                    "Desktop",
                    "Windows",
                    "Chrome",
                    null,
                    null,
                    null)],
                false));

        var result = await context.CreateHandler().HandleAsync(
            new GetLoginActivityHistoryCommand(
                20,
                null,
                new ActorContext(stakeholderId, tenantId, "correlation-id", "flow-id")),
            CancellationToken.None);

        result.Status.ShouldBe(GetLoginActivityHistoryStatus.Success);
        result.Activities.Count.ShouldBe(1);
        result.Activities[0].ActivityType.ShouldBe(nameof(LoginActivityType.InitialLogin));
        result.Activities[0].OccurredAtUtc.ShouldBe(occurredAtUtc);
        result.Activities[0].IpAddress.ShouldBe("203.0.113.10");
        result.Activities[0].City.ShouldBeNull();
        result.NextCursor.ShouldBeNull();
        capturedRequest.ShouldNotBeNull();
        capturedRequest.TenantId.ShouldBe(tenantId);
        capturedRequest.StakeholderId.ShouldBe(stakeholderId);
    }
}
