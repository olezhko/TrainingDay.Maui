using System.Globalization;
using TrainingDay.Maui.Resources.Strings;

namespace TrainingDay.Maui.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    /// Short human-friendly date: "Today", "Yesterday", "3 days ago", "2 weeks ago", otherwise "24 Oct" (with year when not current).
    /// </summary>
    public static string ToRelativeDateString(this DateTime time, DateTime now, CultureInfo culture)
    {
        var days = (now.Date - time.Date).Days;
        if (days <= 0)
            return AppResources.RelativeToday;

        if (days == 1)
            return AppResources.RelativeYesterday;

        if (days < 14)
            return days.ToPluralString(culture, AppResources.RelativeDaysAgoOne, AppResources.RelativeDaysAgoFew, AppResources.RelativeDaysAgoMany);

        if (days < 35)
            return (days / 7).ToPluralString(culture, AppResources.RelativeWeeksAgoOne, AppResources.RelativeWeeksAgoFew, AppResources.RelativeWeeksAgoMany);

        var format = time.Year == now.Year ? "d MMM" : "d MMM yyyy";
        return time.ToString(format, culture);
    }

    /// <summary>
    /// Compact duration: "1 h 05 min", "45 min", or "&lt; 1 min".
    /// </summary>
    public static string ToShortDurationString(this TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours} {AppResources.HourString} {duration.Minutes:00} {AppResources.MinuteString}";

        if (duration.TotalMinutes >= 1)
            return $"{duration.Minutes} {AppResources.MinuteString}";

        return $"< 1 {AppResources.MinuteString}";
    }
}
