using System.Text.Json;

namespace TrueClient;

public static class HttpResult
{
    public static async Task<Result<T>> FromHttpResponseAsync<T>(Func<Task<HttpResponseMessage>> getResponse, Func<HttpResponseMessage, bool>? isSuccessful = null)
    {
        isSuccessful ??= x => x.IsSuccessStatusCode;

        try
        {
            var httpResponse = await getResponse();

            var content = await httpResponse.Content.ReadAsStringAsync();

            if (!isSuccessful(httpResponse))
            {
                return Result.Failure<T>(FormatMessage(httpResponse, $"Response was {(int)httpResponse.StatusCode} with content '{content}'"));
            }

            if (string.IsNullOrEmpty(content))
            {
                return Result.Failure<T>(FormatMessage(httpResponse, $"Response was {(int)httpResponse.StatusCode} but empty."));
            }

            try
            {
                return JsonSerializer.Deserialize<T>(content) is T result
                    ? Result.Success(result)
                    : Result.Failure<T>(FormatMessage(httpResponse, $"Deserialized <null> from '{content}'"));
            }
            catch (JsonException je)
            {
                return Result.Failure<T>(FormatMessage(httpResponse, $"Cannot deserialize response. Error: '{je.Message}' Content '{content}'"));
            }
        }
        catch (Exception e)
        {
            return Result.Failure<T>(e.Message);
        }
    }

    static string FormatMessage(HttpResponseMessage httpResponse, string message) =>
        $"[{httpResponse.RequestMessage?.Method} {httpResponse.RequestMessage?.RequestUri}] {message}";
}
