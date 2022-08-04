using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OmsAuthenticator.Tests.Api.V1
{
    [TestClass]
    public class OmsToken_Post
    {
        [DataTestMethod]
        [DataRow("/oms/token", "")]
        [DataRow("/oms/token", "{}")]
        [DataRow("/oms/token", @"{ ""omsId"": ""best"" }")]
        [DataRow("/oms/token", @"{ ""registrationKey"": ""best"" }")]
        [DataRow("/oms/token", @"{ ""requestId"": ""best"" }")]
        [DataRow("/oms/token", @"{ ""omsId"": ""best"", ""registrationKey"": ""test"" }")]
        [DataRow("/oms/token", @"{ ""omsId"": ""best"", ""requestId"": ""test"" }")]
        [DataRow("/oms/token?omsId=xxx", "{}")]
        [DataRow("/oms/token?registrationKey=xxx", "{}")]
        public async Task GetToken_Invalid_Post(string url, string body)
        {
            // Arrange
            var app = new OmsAuthenticatorApp();
            using var client = app.CreateClient();

            // Act
            using var requestBody = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await app.CreateClient().PostAsync(url, requestBody);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            ResponseShould.BeBadRequest(response, responseContent);
        }

    }
}