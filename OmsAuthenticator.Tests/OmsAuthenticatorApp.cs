using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using OmsAuthenticator.Framework;

namespace OmsAuthenticator.Tests
{
    public class OmsAuthenticatorApp : WebApplicationFactory<Program>
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly SystemTimeMock _systemTimeMock;
        private readonly string _configurationJson;

        public GisMtHelper GisMtApi { get; } = new GisMtHelper();

        public OmsAuthenticatorApp(string configurationJson)
        {
            _systemTimeMock = new SystemTimeMock();

            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpClientFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(() => GisMtApi.GetHttpClient());
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