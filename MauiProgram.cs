using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Plugin.Maui.Audio;
using WaveTuneNew.ViewModels;
using Microsoft.Maui.LifecycleEvents;

namespace WaveTuneNew;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<IAudioManager, AudioManager>();
        builder.Services.AddTransient<LoginViewModel>();

#if WINDOWS
        builder.ConfigureLifecycleEvents(lifecycle =>
        {
            lifecycle.AddWindows(windows =>
            {
                windows.OnWindowCreated(window =>
                {
                    window.Activate();
                    var appWindow = window.AppWindow;
                    appWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));
                    var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(appWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
                    var centeredX = (displayArea.WorkArea.Width - 1200) / 2;
                    var centeredY = (displayArea.WorkArea.Height - 800) / 2;
                    appWindow.Move(new Windows.Graphics.PointInt32(centeredX, centeredY));
                });
            });
        });
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}