using Foundation;

namespace TrainingDay.Maui
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() 
        {
            var app = MauiProgram.CreateMauiApp();
            
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
            Google.MobileAds.MobileAds.SharedInstance.Start(completionHandler: null);
            ObjCRuntime.Class.ThrowOnInitFailure = false;

            return app;
        } 
    }
}
