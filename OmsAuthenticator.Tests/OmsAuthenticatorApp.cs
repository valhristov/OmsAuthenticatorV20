using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using OmsAuthenticator.Framework;
using RichardSzalay.MockHttp;

namespace OmsAuthenticator.Tests
{
    public class OmsAuthenticatorApp : WebApplicationFactory<Program>
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly SystemTimeMock _systemTimeMock;
        private readonly string _configurationJson;

        public OmsTokenApiHelper GisApi { get; }
        public TrueApiTokenApiHelper TrueApi { get; }

        public OmsAuthenticatorApp(string configurationJson)
        {
            _systemTimeMock = new SystemTimeMock();

            var httpClientMock = new MockHttpMessageHandler();

            GisApi = new OmsTokenApiHelper(httpClientMock);
            TrueApi = new TrueApiTokenApiHelper(httpClientMock);

            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpClientFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(() =>
                {
                    var client = httpClientMock.ToHttpClient();
                    client.BaseAddress = new Uri("http://test.com");
                    return client;
                });
            _configurationJson = configurationJson;
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                configBuilder.Sources.Clear();
                configBuilder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(_configurationJson)));
            });
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(_httpClientFactoryMock.Object);
                services.AddSingleton<ISystemTime>(_systemTimeMock);
            });

            return base.CreateHost(builder);
        }

        public void Wait(TimeSpan time) =>
            _systemTimeMock.Wait(time);
    }
}