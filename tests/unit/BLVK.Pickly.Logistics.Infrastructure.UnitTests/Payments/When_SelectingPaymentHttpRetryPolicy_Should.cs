using BLVK.Pickly.Logistics.Infrastructure.Payments;
using Shouldly;
using System.Net;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Payments;

public sealed class When_SelectingPaymentHttpRetryPolicy_Should
{
    [Fact]
    public async Task NotRetryProviderInitializationPost()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "transaction/initialize");
        var policy = ServiceCollectionExtensions.SelectPaymentHttpPolicy(request);
        var executions = 0;

        using var response = await policy.ExecuteAsync(() =>
        {
            executions++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        });

        executions.ShouldBe(1);
    }
}
