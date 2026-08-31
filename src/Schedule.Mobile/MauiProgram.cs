using Microsoft.Extensions.Logging;
using Schedule.Client.Core.Services;
using Schedule.Mobile.Services;

namespace Schedule.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<MobileScheduleSelection>();
#if DEBUG
        // 10.0.2.2 points to the Windows host from the Android emulator.
        builder.Services.AddScheduleClientCore(new Uri("http://10.0.2.2:5274/"));
#else
        builder.Services.AddScheduleClientCore(new Uri("https://api.schedule.nung.edu.ua/"));
#endif

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
