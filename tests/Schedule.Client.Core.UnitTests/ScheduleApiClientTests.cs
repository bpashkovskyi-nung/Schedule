using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Schedule.Client.Core.Api;
using Schedule.Contracts.Schedules;
using Xunit;

namespace Schedule.Client.Core.UnitTests;

public sealed class ScheduleApiClientTests
{
    [Fact]
    public async Task GetScheduleAsync_UsesIsoDatesAndEscapesObjectId()
    {
        HttpRequestMessage? capturedRequest = null;
        using HttpClient httpClient = CreateClient(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ScheduleResponse
                {
                    TargetType = ScheduleTargetType.Group,
                    ObjectId = "group/1",
                    ObjectName = "Група",
                    Period = new(new DateOnly(2026, 8, 24), new DateOnly(2026, 8, 30)),
                    Lessons = [],
                }),
            };
        });

        ScheduleApiClient client = new(httpClient);
        await client.GetScheduleAsync(
            ScheduleTargetType.Group,
            "group/1",
            new DateOnly(2026, 8, 24),
            new DateOnly(2026, 8, 30));

        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.PathAndQuery.Should()
            .Be("/api/v1/schedules/group/group%2F1?from=2026-08-24&to=2026-08-30");
    }

    [Fact]
    public async Task GetReferenceDataAsync_MapsApiErrorResponse()
    {
        using HttpClient httpClient = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = JsonContent.Create(new
            {
                code = "external_schedule_api_error",
                message = "Сервіс розкладу недоступний.",
            }),
        });

        ScheduleApiClient client = new(httpClient);

        Func<Task> action = () => client.GetReferenceDataAsync();
        ScheduleApiException exception = (await action.Should().ThrowAsync<ScheduleApiException>()).Which;

        exception.StatusCode.Should().Be(502);
        exception.Code.Should().Be("external_schedule_api_error");
        exception.Message.Should().Be("Сервіс розкладу недоступний.");
    }

    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        return new HttpClient(new StubHandler(responseFactory))
        {
            BaseAddress = new Uri("https://api.test.local/"),
        };
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
