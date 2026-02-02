using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.Platform;
using SkiaSharp.Views.Maui.Controls.Hosting;
using TrainingDay.Maui.Controls;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels.Pages;
using TrainingDay.Maui.Views;
#if IOS
using Firebase.Crashlytics;
#endif

namespace TrainingDay.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit(static options =>
                {
                    options.SetPopupDefaults(new DefaultPopupSettings
                    {
                        CanBeDismissedByTappingOutsideOfPopup = true,
                        Padding = 4
                    });

                    options.SetPopupOptionsDefaults(new DefaultPopupOptionsSettings
                    {
                        CanBeDismissedByTappingOutsideOfPopup = true,
                    });
                })
                .UseMauiCommunityToolkitMediaElement()
                .UseSkiaSharp()
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
#if IOS
                    handlers.AddHandler<Shell, Platforms.iOS.CustomShellRenderer>();
#endif
                });

            builder.Services.AddSingleton<SettingsPage>();

            builder.Services.AddSingleton<BlogsPageViewModel>();
            builder.Services.AddSingleton<BlogsPage>();

            builder.Services.AddSingleton<TrainingExercisesPageViewModel>();
            builder.Services.AddSingleton<TrainingExercisesPage>();
            builder.Services.AddSingleton<TrainingExercisesMoveOrCopy>();

            builder.Services.AddSingleton<IDataService, DataService>();
            builder.Services.AddSingleton<WorkoutService>();

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

            Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping("Borderless", (handler, entry) =>
            {
#if ANDROID
                handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif

#if IOS
                MapSearchFormatting(handler, entry);
#endif

            });

            Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("Borderless", (handler, entry) =>
            {
#if ANDROID
                handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif

#if IOS
                MapPickerFormatting(handler, entry);
#endif

            });

            builder.RegisterFirebase();

            return builder.Build();
        }

#if IOS

        private static void MapPickerFormatting(IPickerHandler handler, IPicker entry)
        {
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;

            // Update all of the attributed text formatting properties
            handler.PlatformView?.UpdateCharacterSpacing(entry);

            // Setting any of those may have removed text alignment settings,
            // so we need to make sure those are applied, too
            handler.PlatformView?.UpdateHorizontalTextAlignment(entry);
        }

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
        
        private static void MapSearchFormatting(ISearchBarHandler handler, ISearchBar entry)
        {
            handler.PlatformView.SearchBarStyle = UIKit.UISearchBarStyle.Minimal;
        }
#endif
        private static MauiAppBuilder RegisterFirebase(this MauiAppBuilder builder)
        {
            builder.ConfigureLifecycleEvents(events =>
            {
#if IOS
                events.AddiOS(iOS => iOS.FinishedLaunching((app, launchOptions) => {
                    Firebase.Core.App.Configure();
                    Firebase.Crashlytics.Crashlytics.SharedInstance.Init();
                    Firebase.Crashlytics.Crashlytics.SharedInstance.SetCrashlyticsCollectionEnabled(true);
                    Firebase.Crashlytics.Crashlytics.SharedInstance.SendUnsentReports();
                    return false;
                }));
#else
                events.AddAndroid(android => android.OnCreate((activity, bundle) => {
                    Firebase.FirebaseApp.InitializeApp(activity);
                }));
#endif
            });

            return builder;
        }
    }
}
