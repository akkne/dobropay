namespace Dobropay.UnitTests.MerchantAdmin;

public sealed class TestHttpClientFactory(HttpClient client) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => client;
}
