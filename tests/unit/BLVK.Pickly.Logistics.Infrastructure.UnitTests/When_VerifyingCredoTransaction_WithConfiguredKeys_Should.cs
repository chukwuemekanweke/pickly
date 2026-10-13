using BLVK.Pickly.Logistics.Infrastructure.Payments.Credo;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Net;
using System.Text;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class When_VerifyingCredoTransaction_WithConfiguredKeys_Should
{
    [Fact]
    public async Task UseSecretKeyAuthorizationHeader()
    {
        var handler = new CapturingMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {"status":200,"message":"Successfully processed","data":{"businessCode":"700607001390003","transRef":"provider-ref","businessRef":"merchant-ref","debitedAmount":1500,"transAmount":1500,"transFeeAmount":0,"settlementAmount":1500,"customerId":"ada@example.com","transactionDate":"2026-05-01 00:00:00","channelId":0,"currencyCode":"NGN","status":0,"metadata":[]},"execTime":0,"error":[]}
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

        await sut.VerifyTransactionAsync("merchant-ref", CancellationToken.None);

        handler.Request.ShouldNotBeNull();
        handler.Request!.Method.ShouldBe(HttpMethod.Get);
        handler.Request.Headers.GetValues("Authorization").Single().ShouldBe("test_secret_key");
        handler.Request.RequestUri!.AbsolutePath.ShouldBe("/transaction/merchant-ref/verify");
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
