using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Plugin.AdMob;
using SkiaSharp.Views.Maui.Controls.Hosting;
using TrainingDay.Maui.Controls;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseSkiaSharp()
                .UseAdMob()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("Inter-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("Inter-Semibold.ttf", "OpenSansSemibold");
                })
                .ConfigureEffects(effects =>
                {
                    effects.Add<LongPressedEffect, LongPressedPlatformEffect>();
                })
                .ConfigureMauiHandlers(handlers =>
                {
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

#if ANDROID
            builder.Services.AddTransient<IPushNotification, Platforms.Android.PushNotificationService>();
#endif

            return builder.Build();
        }
    }
}
