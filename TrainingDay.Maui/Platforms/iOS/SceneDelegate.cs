using Foundation;
using UIKit;

namespace TrainingDay.Maui
{
    [Register("SceneDelegate")]
    public class SceneDelegate : MauiUISceneDelegate
    {
        public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
        {
            base.WillConnect(scene, session, connectionOptions);

            // Cold start from a shared file: URLs arrive here instead of AppDelegate.OpenUrl
            HandleIncomingUrls(connectionOptions.UrlContexts);
        }

        public override bool OpenUrl(UIScene scene, NSSet<UIOpenUrlContext> urlContexts)
        {
            var handled = base.OpenUrl(scene, urlContexts);
            HandleIncomingUrls(urlContexts);
            return handled;
        }

        private static void HandleIncomingUrls(NSSet<UIOpenUrlContext>? urlContexts)
        {
            if (urlContexts == null)
                return;

            foreach (var context in urlContexts)
            {
                var url = context.Url;
                if (!url.IsFileUrl || url.Path == null)
                    continue;

                bool accessGranted = url.StartAccessingSecurityScopedResource();
                try
                {
                    var fileContent = File.ReadAllText(url.Path);
                    (App.Current as App)?.SetIncomingFile(fileContent);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
                finally
                {
                    if (accessGranted)
                        url.StopAccessingSecurityScopedResource();
                }
            }
        }
    }
}
