using Schedule.Client.Core.Services;
using Schedule.Web.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
Uri apiBaseAddress = new(builder.Configuration["ApiBaseUrl"] ?? "https://api.schedule.ksm.if.ua/");
builder.Services.AddScheduleClientCore(apiBaseAddress);
builder.Services.AddSingleton<PeriodOptionsProvider>();
builder.Services.AddScoped<IScheduleReferenceService, ScheduleReferenceService>();
builder.Services.AddScoped<IScheduleQueryService, ScheduleQueryService>();

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
    name: "privacy-policy",
    pattern: "privacy-policy",
    defaults: new { controller = "Home", action = "Privacy" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
