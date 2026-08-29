using Dekanat.ScheduleSdk.DependencyInjection;
using Schedule.Api.Configuration;
using Schedule.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.Configure<ScheduleApiOptions>(
    builder.Configuration.GetSection(ScheduleApiOptions.SectionName));
builder.Services.AddPsRozkladClient();
builder.Services.AddScoped<IScheduleApiService, ScheduleApiService>();
builder.Services.AddHttpClient<ITimetableSuggestionsService, TimetableSuggestionsService>();
builder.Services.AddMemoryCache();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Web");
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();
