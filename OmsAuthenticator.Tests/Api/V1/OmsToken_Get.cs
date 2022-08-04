using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OmsAuthenticator.Tests.Api.V1
{
    [TestClass]
    public class OmsToken_Get
    {
        [DataTestMethod]
        [DataRow("/oms/token")]
        [DataRow("/oms/token?test=best")]
        [DataRow("/oms/token?omsId=xxx")]
        [DataRow("/oms/token?registrationKey=xxx")]
        public async Task GetToken_Invalid_Get(string url)
        {
            // Arrange
            var app = new OmsAuthenticatorApp();

            using var client = app.CreateClient();

            // Act
            using var response = await client.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            ResponseShould.BeBadRequest(response, responseContent);
        }
    }
}