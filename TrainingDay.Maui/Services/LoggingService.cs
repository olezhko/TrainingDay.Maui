using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace TrainingDay.Maui.Services
{
    public static class LoggingService
    {
        public static void TrackError(Exception ex, IDictionary<string, string> properties)
        {
            Crashes.TrackError(ex, properties);
            //var parameters = new Bundle();
            //parameters.PutString("error_message", ex.Message);
            //parameters.PutString("stack_trace", ex.StackTrace);

            //var context = Android.App.Application.Context;
            //FirebaseAnalytics.GetInstance(context).LogEvent("app_error", parameters);
        }

        public static void TrackEvent(string text, IDictionary<string, string> properties)
        {
            Analytics.TrackEvent(text, properties);
        }
    }
}
