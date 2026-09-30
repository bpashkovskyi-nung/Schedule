using System.Threading.RateLimiting;
using Dekanat.ScheduleSdk.DependencyInjection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Schedule.Api.Configuration;
using Schedule.Api.Database;
using Schedule.Api.Middleware;
using Schedule.Api.Services;
using Schedule.Contracts.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.Configure<ScheduleApiOptions>(
    builder.Configuration.GetSection(ScheduleApiOptions.SectionName));
builder.Services.Configure<ScheduleSourceOptions>(
    builder.Configuration.GetSection(ScheduleSourceOptions.SectionName));
builder.Services.Configure<ScheduleSyncOptions>(
    builder.Configuration.GetSection(ScheduleSyncOptions.SectionName));

int externalTimeoutSeconds = Math.Max(
    1,
    builder.Configuration.GetValue<int?>($"{ScheduleApiOptions.SectionName}:ExternalRequestTimeoutSeconds") ?? 60);

ScheduleSourceOptions scheduleSource = new();
builder.Configuration.GetSection(ScheduleSourceOptions.SectionName).Bind(scheduleSource);

builder.Services.AddPsRozkladClient(options =>
{
    options.RequestTimeout = TimeSpan.FromSeconds(externalTimeoutSeconds);
});

if (scheduleSource.IsDatabase)
{
    string? connectionString = builder.Configuration.GetConnectionString(scheduleSource.ConnectionStringName);
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            $"Connection string '{scheduleSource.ConnectionStringName}' is required when ScheduleSource:Provider is Database.");
    }

    builder.Services.AddDbContextFactory<ScheduleDbContext>(options => options.UseSqlServer(connectionString));
    builder.Services.AddScoped<IScheduleDataSource, DatabaseScheduleDataSource>();
    builder.Services.AddScoped<IScheduleSynchronizer, DekanatScheduleSynchronizer>();
    builder.Services.AddHostedService<ScheduleSyncBackgroundService>();
}
else if (string.Equals(scheduleSource.Provider, "Dekanat", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IScheduleDataSource, DekanatScheduleDataSource>();
}
else
{
    throw new InvalidOperationException(
        $"Unsupported ScheduleSource:Provider '{scheduleSource.Provider}'. Use 'Dekanat' or 'Database'.");
}

builder.Services.AddScoped<IScheduleApiService, ScheduleApiService>();
builder.Services.AddTransient<TransientRetryHandler>();
builder.Services.AddHttpClient<ITimetableSuggestionsService, TimetableSuggestionsService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(externalTimeoutSeconds);
}).AddHttpMessageHandler<TransientRetryHandler>();
builder.Services.AddMemoryCache();

int workloadPermitLimit = Math.Max(
    1,
    builder.Configuration.GetValue<int?>($"{ScheduleApiOptions.SectionName}:WorkloadPermitLimit") ?? 2);
int workloadWindowSeconds = Math.Max(
    1,
    builder.Configuration.GetValue<int?>($"{ScheduleApiOptions.SectionName}:WorkloadWindowSeconds") ?? 30);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("workload", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown-client",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = workloadPermitLimit,
            Window = TimeSpan.FromSeconds(workloadWindowSeconds),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        context.HttpContext.Response.Headers.RetryAfter = workloadWindowSeconds.ToString();
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ApiErrorResponse(
                "rate_limit_exceeded",
                "Забагато дорогих запитів. Повторіть спробу пізніше.",
                context.HttpContext.TraceIdentifier),
            cancellationToken);
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Web", policy =>
    {
        string[] origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length > 0)
        {
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var app = builder.Build();

app.UseMiddleware<ApiExceptionMiddleware>();

if (builder.Configuration.GetValue(
        $"{ScheduleApiOptions.SectionName}:EnableOpenApi",
        app.Environment.IsDevelopment()))
{
    app.MapOpenApi();
}

app.UseRouting();
app.UseCors("Web");
app.UseRateLimiter();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();
