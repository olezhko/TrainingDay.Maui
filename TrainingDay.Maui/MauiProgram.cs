using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
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

#if IOS
            builder.Services.AddTransient<IPushNotification, Platforms.iOS.PushNotificationService>();
#endif

            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderLine", (handler, entry) =>
            {
#if ANDROID
                handler.PlatformView.SetHighlightColor(Android.Graphics.Color.Transparent);
                handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif

#if IOS
            MapFormatting(handler, entry);
#endif

            });

            #region IOS
            Plugin.AdMob.Configuration.AdConfig.DefaultBannerAdUnitId = Extensions.ConstantKeys.WorkoutiOSAds;
            #endregion

            return builder.Build();
        }

#if IOS
        private static void MapFormatting(IEntryHandler handler, IEntry entry)
        {
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;

            handler.PlatformView?.UpdateMaxLength(entry);

            // Update all of the attributed text formatting properties
            handler.PlatformView?.UpdateCharacterSpacing(entry);

            // Setting any of those may have removed text alignment settings,
            // so we need to make sure those are applied, too
            handler.PlatformView?.UpdateHorizontalTextAlignment(entry);
        }
#endif

    }
}
