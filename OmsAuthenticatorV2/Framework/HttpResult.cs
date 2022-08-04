using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace OmsAuthenticator.Framework
{
    public static class HttpResult
    {
        public static async Task<Result<T>> FromHttpResponseAsync<T>(Func<Task<HttpResponseMessage>> getResponse)
        {
            try
            {
                var httpResponse = await getResponse();

                var content = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    return Result.Failure<T>($"StatusCode: {httpResponse.StatusCode}. Response content: '{content}'");
                }

                if (string.IsNullOrEmpty(content))
                {
                    return Result.Failure<T>($"Status code: {httpResponse.StatusCode}. Response content is empty");
                }

                try
                {
                    return JsonSerializer.Deserialize<T>(content) is T result
                        ? Result.Success(result)
                        : Result.Failure<T>($"Deserialized <null> from '{content}'");
                }
                catch (JsonException je)
                {
                    return Result.Failure<T>($"Error deserializing '{content}': {je.Message}");
                }
            }
            catch (Exception e)
            {
                return Result.Failure<T>(e.Message);
            }
        }
    }
}
