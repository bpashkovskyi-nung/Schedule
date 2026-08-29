using System.Net;
using Microsoft.Extensions.Options;
using Schedule.Api.Configuration;

namespace Schedule.Api.Services;

public sealed class TransientRetryHandler(
    IOptions<ScheduleApiOptions> options,
    ILogger<TransientRetryHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        int retryCount = Math.Max(0, options.Value.ExternalRetryCount);

        for (int attempt = 0; ; attempt++)
        {
            try
            {
                HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
                if (!ShouldRetry(request, response.StatusCode) || attempt >= retryCount)
                {
                    return response;
                }

                response.Dispose();
            }
            catch (HttpRequestException) when (request.Method == HttpMethod.Get && attempt < retryCount)
            {
                logger.LogWarning(
                    "Transient suggestions request failure; retry {Attempt} of {RetryCount}",
                    attempt + 1,
                    retryCount);
            }

            TimeSpan delay = TimeSpan.FromMilliseconds(200 * (attempt + 1));
            await Task.Delay(delay, cancellationToken);
        }
    }

    private static bool ShouldRetry(HttpRequestMessage request, HttpStatusCode statusCode) =>
        request.Method == HttpMethod.Get &&
        statusCode is HttpStatusCode.RequestTimeout or
            HttpStatusCode.TooManyRequests or
            HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.GatewayTimeout;
}
