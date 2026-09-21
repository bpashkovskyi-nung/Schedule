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
        builder.Services.AddSingleton<MobileScheduleSettingsStore>();
        builder.Services.AddSingleton(sp => sp.GetRequiredService<MobileScheduleSettingsStore>().Load());
        builder.Services.AddSingleton<MobileNotificationScheduler>();
        // The mobile app uses the same live API in Debug and Release.
        builder.Services.AddScheduleClientCore(MobileApiConfiguration.BaseUri);

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
