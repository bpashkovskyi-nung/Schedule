using Schedule.Web.Components;
using Schedule.Client.Core.Services;

var builder = WebApplication.CreateBuilder(args);

Uri apiBaseAddress = new(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7160/");
builder.Services.AddScheduleClientCore(apiBaseAddress);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Schedule.Client.UI.Routes).Assembly);

app.Run();
