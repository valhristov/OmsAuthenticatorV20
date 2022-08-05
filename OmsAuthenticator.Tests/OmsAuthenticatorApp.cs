﻿using Microsoft.AspNetCore.Mvc.Testing;
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
        private SystemTimeMock _systemTimeMock;

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

            _systemTimeMock = new SystemTimeMock();
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
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