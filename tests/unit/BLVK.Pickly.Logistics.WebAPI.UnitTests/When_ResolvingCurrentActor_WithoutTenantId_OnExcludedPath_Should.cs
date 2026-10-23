using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests;

public sealed class When_ResolvingCurrentActor_WithoutTenantId_OnExcludedPath_Should
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/readiness")]
    [InlineData("/metrics")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar")]
    [InlineData("/scalar/v1")]
    [InlineData("/")]
    [InlineData("/api/v1/payments/webhooks/credo")]
    [InlineData("/api/v1/payments/webhooks/safehaven")]
    [InlineData("/api/v1/email-notifications/webhooks/mailtrap")]
    [InlineData("/api/v1/operations/metrics/snapshot")]
    [InlineData("/api/v1/operations/metrics/time-series")]
    [InlineData("/operations")]
    public async Task AllowRequest(string path)
    {
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var stakeholderRepository = Substitute.For<IStakeholderReadModelRepository>();
        var problemDetailsService = Substitute.For<IProblemDetailsService>();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;

        var nextWasCalled = false;
        var sut = new CurrentActorMiddleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });

        await sut.InvokeAsync(httpContext, currentActorAccessor, stakeholderRepository, problemDetailsService);

        nextWasCalled.ShouldBeTrue();
        await problemDetailsService.DidNotReceive().WriteAsync(Arg.Any<ProblemDetailsContext>());
    }
}
