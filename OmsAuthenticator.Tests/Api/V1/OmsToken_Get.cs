using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OmsAuthenticator.Tests.Api.V1
{
    [TestClass]
    public class OmsToken_Get
    {
        private readonly OmsAuthenticatorApp _app;
        private readonly HttpClient _client;

        public OmsToken_Get()
        {
            _app = new OmsAuthenticatorApp(() => default!);
            _client = _app.CreateClient();
        }

        [DataTestMethod]
        [DataRow("/oms/token")]
        [DataRow("/oms/token?test=best")]
        [DataRow("/oms/token?omsId=xxx")]
        [DataRow("/oms/token?registrationKey=xxx")]
        public async Task GetToken_Invalid_Get(string url)
        {
            // Arrange

            // Act
            using var response = await _client.GetAsync(url);

            // Assert
            await ResponseShould.BeBadRequest(response);
        }

    }
}