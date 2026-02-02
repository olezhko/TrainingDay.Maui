#if IOS
using CoreData;
using Foundation;
#endif

namespace TrainingDay.Maui.Services
{
    public static class LoggingService
    {
        public const string StartEvent = "App_Start";

        private static readonly Guid SessionId;
        static LoggingService()
        {
            SessionId = Guid.NewGuid();
        }

        public static void TrackError(Exception ex, IDictionary<string, string> properties = null)
        {
#if ANDROID
            var keyAndValue = new Firebase.Crashlytics.CustomKeysAndValues.Builder();
            keyAndValue.PutString(nameof(SessionId), SessionId.ToString());
            foreach (var param in properties ?? new Dictionary<string, string>())
            {
                keyAndValue.PutString(param.Key, param.Value);
            }

            Firebase.Crashlytics.FirebaseCrashlytics.Instance.RecordException(Java.Lang.Throwable.FromException(ex), keyAndValue.Build());
#elif IOS

            var userInfo = new NSMutableDictionary
            {
                { new NSString("Session"), new NSString(SessionId.ToString()) },
                { new NSString("Message"), new NSString(ex.Message) },
                { new NSString("StackTrace"), new NSString(ex.StackTrace ?? "empty stack trace") }
            };

            if (properties != null)
            {
                foreach (var param in properties)
                {
                    userInfo.Add(new NSString(param.Key), new NSString(param.Value));
                }
            }

            Firebase.Crashlytics.Crashlytics.SharedInstance.RecordError(new Foundation.NSError(new NSString("TrainingDay"), ex.HResult, userInfo));
#endif
        }

        public static void TrackEvent(string text, IDictionary<string, string> properties = null)
        {
#if ANDROID
            var firebaseAnalytics = Firebase.Analytics.FirebaseAnalytics.GetInstance(Platform.CurrentActivity);

            if (properties == null)
            {
                firebaseAnalytics.LogEvent(text, null);
                return;
            }

            var bundle = new Android.OS.Bundle();
            foreach (var param in properties)
            {
                bundle.PutString(param.Key, param.Value);
                bundle.PutString(nameof(SessionId), SessionId.ToString());
            }

            firebaseAnalytics.LogEvent(text, bundle);
#elif IOS
            Firebase.Crashlytics.Crashlytics.SharedInstance.Log(text);
#endif
        }
    }
}
