using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shouldly;
using System.Diagnostics;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Infrastructure;

public sealed class When_HandlingUnhandledException_Should
{
    [Fact]
    public async Task MarkTheActiveSpanAsErrorAndRecordTheException()
    {
        var logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
        var problemDetailsService = Substitute.For<IProblemDetailsService>();
        var handler = new GlobalExceptionHandler(logger, problemDetailsService);
        var httpContext = new DefaultHttpContext();
        var exception = new InvalidOperationException("Sensitive internal failure detail.");

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        using var activitySource = new ActivitySource(nameof(When_HandlingUnhandledException_Should));
        using var activity = activitySource.StartActivity("test-request");

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        activity.ShouldNotBeNull();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.Events.ShouldContain(activityEvent => activityEvent.Name == "exception");
    }
}
