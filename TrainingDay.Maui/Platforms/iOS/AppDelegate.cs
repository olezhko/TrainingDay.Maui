using Foundation;
using UIKit;

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

            Firebase.Core.App.Configure();

            return app;
        }

        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            if (Platform.OpenUrl(app, url, options))
                return true;

            var fileContent = File.ReadAllText(url.Path);
            (App.Current as App).SetIncomingFile(fileContent);
            return true;
        }
    }
}
