using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using OmsAuthenticator.ApiAdapters.GISMT.V3;

namespace OmsAuthenticator.Tests
{
    public class OmsAuthenticatorApp : WebApplicationFactory<Program>
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;

        public OmsAuthenticatorApp(Func<HttpClient> getHttpClient)
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpClientFactoryMock
                .Setup(x => x.CreateClient(OmsTokenAdapter.HttpClientName))
                .Returns(() =>
                {
                    var client = getHttpClient();
                    client.BaseAddress = new Uri("http://test.com");
                    return client;
                });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(_httpClientFactoryMock.Object);
            });

            return base.CreateHost(builder);
        }
    }
}