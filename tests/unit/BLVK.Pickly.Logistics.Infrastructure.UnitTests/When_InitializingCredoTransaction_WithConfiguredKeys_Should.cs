using BLVK.Pickly.Logistics.Infrastructure.Payments.Credo;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Net;
using System.Text;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class When_InitializingCredoTransaction_WithConfiguredKeys_Should
{
    [Fact]
    public async Task UsePublicKeyAuthorizationHeader()
    {
        var handler = new CapturingMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {"status":0,"message":"ok","data":{"authorizationUrl":"https://checkout.local/pay","reference":"merchant-ref","credoReference":"credo-ref","crn":"crn-123"},"execTime":0,"error":[]}
                """,
                Encoding.UTF8,
                "application/json")
        });
        var sut = new CredoClient(
            new FakeHttpClientFactory(new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.credodemo.com")
            }),
            Options.Create(new CredoOptions
            {
                PublicKey = "0PUB_test_public_key",
                SecretKey = "test_secret_key"
            }));

        await sut.InitializeTransactionAsync(
            new CredoInitializeTransactionRequest(
                1500,
                InfrastructureTestData.Email(),
                null,
                InfrastructureTestData.FirstName(),
                InfrastructureTestData.LastName(),
                "NGN",
                "merchant-ref",
                "WalletTopUp payment"),
            CancellationToken.None);

        handler.Request.ShouldNotBeNull();
        handler.Request!.Method.ShouldBe(HttpMethod.Post);
        handler.Request.Headers.GetValues("Authorization").Single().ShouldBe("0PUB_test_public_key");
        handler.Request.RequestUri!.AbsolutePath.ShouldBe("/transaction/initialize");
    }

    private sealed class FakeHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => httpClient;
    }

    private sealed class CapturingMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(responseFactory(request));
        }
    }
}
