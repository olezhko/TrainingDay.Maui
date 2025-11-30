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

#endif
        }
    }
}
