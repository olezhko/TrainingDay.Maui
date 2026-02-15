using SentinelAnalytics.Maui;

namespace TrainingDay.Maui.Services
{
    public static class LoggingService
    {
        public static async Task TrackError(Exception ex, IDictionary<string, string>? properties = null) 
            => await SentinelTracker.TrackErrorAsync(ex, properties: (IDictionary<string, object>?)properties);

        public static async Task TrackEvent(string text, IDictionary<string, string>? properties = null) 
            => await SentinelTracker.TrackEventAsync(text, properties: (IDictionary<string, object>?)properties);
    }
}