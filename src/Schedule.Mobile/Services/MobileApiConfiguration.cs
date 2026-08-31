namespace Schedule.Mobile.Services;

internal static class MobileApiConfiguration
{
    public static Uri BaseUri =>
#if DEBUG
        new Uri("http://10.0.2.2:5274/");
#else
        new Uri("https://api.schedule.ksm.if.ua/");
#endif
}
