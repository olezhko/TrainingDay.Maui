using Android.App;
using Android.Content.PM;
using Android.Gms.Ads;
using Android.OS;
using Firebase.Iid;

namespace TrainingDay.Maui
{
    [Activity(Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density, LaunchMode = LaunchMode.SingleTop)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

#if DEBUG
            var configBuilder = new RequestConfiguration.Builder();
            configBuilder.SetTestDeviceIds(new List<string>() { "558BEEA7EC8B11BD288CD4BC81AACA59", "1AD07ECCFB97DD84BD8554BD4E4349CA" });
            MobileAds.RequestConfiguration = configBuilder.Build();
#endif
            CreateNotificationChannel();

            var refreshedToken = FirebaseInstanceId.Instance.Token;
            App.SendRegistrationToServer(refreshedToken);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnPostCreate(Bundle? savedInstanceState)
        {
            base.OnPostCreate(savedInstanceState);
        }

        internal static readonly string AppChannel = "Application";
        internal static readonly string SilentChannel = "Implementation";
        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Notification channels are new in API 26 (and not a part of the
                // support library). There is no need to create a notification 
                // channel on older versions of Android.
                return;
            }

            var channel = new NotificationChannel(AppChannel, AppChannel, NotificationImportance.Max)
            {
                Description = AppChannel
            };
            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(channel);


            var silent_channel = new NotificationChannel(SilentChannel, SilentChannel, NotificationImportance.Low)
            {
                Description = SilentChannel
            };
            notificationManager.CreateNotificationChannel(silent_channel);
        }
    }
}
