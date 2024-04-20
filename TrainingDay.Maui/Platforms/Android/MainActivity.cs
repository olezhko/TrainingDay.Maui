using Android.App;
using Android.Content.PM;
using Android.Gms.Ads;
using Android.OS;

namespace TrainingDay.Maui
{
    [Activity(Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var configBuilder = new RequestConfiguration.Builder();

            configBuilder.SetTestDeviceIds(new List<string>() { "558BEEA7EC8B11BD288CD4BC81AACA59" });

            MobileAds.RequestConfiguration = configBuilder.Build();
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
    }
}
