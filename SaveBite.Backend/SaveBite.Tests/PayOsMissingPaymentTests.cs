using System.Net;
using System.Text;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Implementations;
using Xunit;

namespace SaveBite.Tests;

public class PayOsMissingPaymentTests
{
    private static readonly PlatformPayOsCredentials Credentials = new("client", "api", "checksum");

    [Fact]
    public async Task SuccessfulHttpResponseWithCode101MeansMissingPaymentForLookup()
    {
        using var factory = new Factory(HttpStatusCode.OK, "101");
        var result = await new PayOsApiClient(factory).SendAsync(HttpMethod.Get, "v2/payment-requests/123456", Credentials, null, default);
        Assert.Equal("NOT_FOUND", result.GetProperty("status").GetString());
    }

    [Theory]
    [InlineData("POST", "v2/payment-requests", 200, "101")]
    [InlineData("POST", "v2/payment-requests/123456/cancel", 200, "101")]
    [InlineData("GET", "v2/payment-requests/123456", 401, "101")]
    [InlineData("GET", "v2/payment-requests/123456", 500, "101")]
    [InlineData("GET", "v2/payment-requests/123456", 200, "99")]
    [InlineData("GET", "v2/payment-requests/123456/invoices", 200, "101")]
    public async Task OtherErrorsAreNeverTreatedAsMissingPayment(string method, string path, int status, string code)
    {
        using var factory = new Factory((HttpStatusCode)status, code);
        await Assert.ThrowsAnyAsync<AppException>(() => new PayOsApiClient(factory).SendAsync(new HttpMethod(method), path, Credentials, null, default));
    }

    private sealed class Factory : IHttpClientFactory, IDisposable
    {
        private readonly HttpClient client;
        public Factory(HttpStatusCode status, string code) => client = new HttpClient(new Handler(status, code)) { BaseAddress = new Uri("https://api-merchant.payos.vn/") };
        public HttpClient CreateClient(string name) => client;
        public void Dispose() => client.Dispose();
    }
    private sealed class Handler(HttpStatusCode status, string code) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => Task.FromResult(new HttpResponseMessage(status)
        { Content = new StringContent($$"""{"code":"{{code}}","desc":"Mã thanh toán không tồn tại","data":null}""", Encoding.UTF8, "application/json") });
    }
}
