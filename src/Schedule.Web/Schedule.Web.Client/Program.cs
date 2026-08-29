using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Schedule.Client.Core.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

Uri apiBaseAddress = new(builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress);
builder.Services.AddScheduleClientCore(apiBaseAddress);

await builder.Build().RunAsync();
