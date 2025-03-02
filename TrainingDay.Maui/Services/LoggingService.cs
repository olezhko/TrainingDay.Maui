#if IOS

using Firebase.Analytics;
using Foundation;

#endif

namespace TrainingDay.Maui.Services
{
    public static class LoggingService
    {
        private static string UserId = Guid.NewGuid().ToString();
        public static string ApplicationStarted = nameof(ApplicationStarted);
        public static string ExerciseChangedMessage = nameof(ExerciseChangedMessage);
        public static string FilterAcceptedForExercisesMessage = nameof(FilterAcceptedForExercisesMessage);
        public static string PreparedTrainingsAddExercisesFinished = nameof(PreparedTrainingsAddExercisesFinished);

        public static void TrackError(Exception ex, IDictionary<string, string> properties = null)
        {
            properties = InitProperties(properties);

#if IOS
            List<NSString> keys = new List<NSString>();
            List<NSString> values = new List<NSString>();
            foreach (var prop in properties)
            {
                keys.Add(new NSString(prop.Key));
                values.Add(new NSString(prop.Value));
            }

            keys.Add(new NSString("StackTrace"));
            values.Add(new NSString(ex.StackTrace));

            keys.Add(new NSString("session"));
            values.Add(new NSString(UserId));

            var parameters = NSDictionary<NSString, NSObject>.FromObjectsAndKeys(values.ToArray(), keys.ToArray(), properties.Count + 1);
            Analytics.LogEvent(ex.Message, parameters);

            Firebase.Crashlytics.Crashlytics.SharedInstance.RecordExceptionModel(new Firebase.Crashlytics.ExceptionModel(ex.Message,ex.StackTrace)
            {
                
            });
#else
            //var parameters = new Bundle();
            //parameters.PutString("error_message", ex.Message);
            //parameters.PutString("stack_trace", ex.StackTrace);

            //var context = Android.App.Application.Context;
            //FirebaseAnalytics.GetInstance(context).LogEvent("app_error", parameters);

#endif
            Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
            
        }

        public static void TrackEvent(string text, IDictionary<string, string> properties = null)
        {
            properties = InitProperties(properties);
#if IOS
            List<NSString> keys = new List<NSString>();
            List<NSString> values = new List<NSString>();
            foreach (var prop in properties)
            {
                keys.Add(new NSString(prop.Key));
                values.Add(new NSString(prop.Value));
            }

            keys.Add(new NSString("session"));
            values.Add(new NSString(UserId));

            var parameters = NSDictionary<NSString, NSObject>.FromObjectsAndKeys(values.ToArray(), keys.ToArray(), properties.Count + 1);
            Analytics.LogEvent(text.Substring(0, Math.Min(40, text.Length)), parameters);
#else
            //var parameters = new Bundle();
            //parameters.PutString("error_message", ex.Message);
            //parameters.PutString("stack_trace", ex.StackTrace);

            //var context = Android.App.Application.Context;
            //FirebaseAnalytics.GetInstance(context).LogEvent("app_error", parameters);
#endif

            Microsoft.AppCenter.Analytics.Analytics.TrackEvent(text, properties);
        }


        private static IDictionary<string, string> InitProperties(IDictionary<string, string> properties)
        {
            if (properties is null)
            {
                properties = new Dictionary<string, string>();
            }

            if (properties is not null)
            {
                properties.Add("session", UserId);
            }

            return properties;
        }
    }
}
