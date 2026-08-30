using Dekanat.ScheduleSdk.DependencyInjection;
using Schedule.Web.Configuration;
using Schedule.Web.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddPsRozkladClient();
builder.Services.Configure<LegacyFeatureOptions>(
    builder.Configuration.GetSection(LegacyFeatureOptions.SectionName));

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
