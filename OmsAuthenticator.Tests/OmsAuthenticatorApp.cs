using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using OmsAuthenticator.ApiAdapters.GISMT.V3;
using OmsAuthenticator.Framework;

namespace OmsAuthenticator.Tests
{
    public class OmsAuthenticatorApp : WebApplicationFactory<Program>
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<ISystemTime> _systemTimeMock;

        private DateTimeOffset _currentTime = DateTimeOffset.UtcNow;

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

            _systemTimeMock = new Mock<ISystemTime>();
            _systemTimeMock.SetupGet(x => x.UtcNow).Returns(() => _currentTime);
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(_httpClientFactoryMock.Object);
                services.AddSingleton(_systemTimeMock.Object);
            });

            return base.CreateHost(builder);
        }

        public void Wait(TimeSpan time) =>
            _currentTime += time;
    }
}