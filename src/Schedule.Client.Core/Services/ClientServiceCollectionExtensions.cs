using Microsoft.Extensions.DependencyInjection;
using Schedule.Client.Core.Api;

namespace Schedule.Client.Core.Services;

public static class ClientServiceCollectionExtensions
{
    public static IServiceCollection AddScheduleClientCore(
        this IServiceCollection services,
        Uri apiBaseAddress)
    {
        services.AddScoped(_ => new HttpClient { BaseAddress = apiBaseAddress });
        services.AddScoped<IScheduleApiClient, ScheduleApiClient>();
        services.AddScoped<ReferenceDataState>();
        services.AddScoped<ScheduleState>();
        return services;
    }
}
