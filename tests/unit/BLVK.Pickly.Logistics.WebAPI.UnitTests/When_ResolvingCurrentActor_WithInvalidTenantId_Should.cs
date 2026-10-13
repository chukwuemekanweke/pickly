using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests;

public sealed class When_ResolvingCurrentActor_WithInvalidTenantId_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var stakeholderRepository = Substitute.For<IStakeholderReadModelRepository>();
        var problemDetailsService = Substitute.For<IProblemDetailsService>();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/v1/test";
        httpContext.Request.Headers["X-Tenant-Id"] = "not-a-guid";

        var nextWasCalled = false;
        var sut = new CurrentActorMiddleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });

        await sut.InvokeAsync(httpContext, currentActorAccessor, stakeholderRepository, problemDetailsService);

        nextWasCalled.ShouldBeFalse();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        await problemDetailsService.Received(1).WriteAsync(Arg.Any<ProblemDetailsContext>());
    }
}
