using SentinelAnalytics.Maui;

namespace TrainingDay.Maui.Services
{
    public static class LoggingService
    {
        public static void TrackError(Exception ex, IDictionary<string, string>? properties = null)
            => SentinelTracker.TrackError(ex, properties: (IDictionary<string, object>?)properties);

        public static void TrackEvent(string text, IDictionary<string, string>? properties = null)
            => SentinelTracker.TrackEvent(text, properties: (IDictionary<string, object>?)properties);
    }
}