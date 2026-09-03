using Dekanat.ScheduleSdk.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Schedule.Web.Configuration;
using Schedule.Web.Database;
using Schedule.Web.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddPsRozkladClient();
builder.Services.Configure<LegacyFeatureOptions>(
    builder.Configuration.GetSection(LegacyFeatureOptions.SectionName));
builder.Services.Configure<ScheduleSourceOptions>(
    builder.Configuration.GetSection(ScheduleSourceOptions.SectionName));

ScheduleSourceOptions scheduleSource = new();
builder.Configuration.GetSection(ScheduleSourceOptions.SectionName).Bind(scheduleSource);
if (scheduleSource.IsDatabase)
{
    string connectionString = builder.Configuration.GetConnectionString(scheduleSource.ConnectionStringName)
                               ?? throw new InvalidOperationException(
                                   $"Connection string '{scheduleSource.ConnectionStringName}' is required when ScheduleSource:Provider is Database.");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            $"Connection string '{scheduleSource.ConnectionStringName}' is required when ScheduleSource:Provider is Database.");
    }

    builder.Services.AddDbContextFactory<ScheduleDbContext>(options => options.UseSqlServer(connectionString));
    builder.Services.AddScoped<IScheduleRepository, DatabaseScheduleRepository>();
}
else if (string.Equals(scheduleSource.Provider, "Dekanat", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IScheduleRepository, DekanatScheduleRepository>();
}
else
{
    throw new InvalidOperationException(
        $"Unsupported ScheduleSource:Provider '{scheduleSource.Provider}'. Use 'Dekanat' or 'Database'.");
}

builder.Services.AddHttpClient<ITimetableSuggestionsService, TimetableSuggestionsService>();
builder.Services.AddSingleton<PeriodOptionsProvider>();
builder.Services.AddSingleton<ILegacyIcalGenerator, LegacyIcalGenerator>();
builder.Services.AddScoped<IScheduleReferenceService, ScheduleReferenceService>();
builder.Services.AddScoped<IScheduleQueryService, ScheduleQueryService>();
builder.Services.AddScoped<IAuditoriumLoadService, AuditoriumLoadService>();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
