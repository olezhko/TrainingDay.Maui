using System.Globalization;

namespace TrainingDay.Maui.Extensions;

public static class PluralExtensions
{
    /// <summary>
    /// Picks the plural form for <paramref name="count"/> and fills it in.
    /// Russian uses one / few / many ("1 упражнение", "2 упражнения", "5 упражнений");
    /// the other supported languages (en, de, es, pt) only distinguish one / other, so their resources repeat the same text for few and many.
    /// </summary>
    public static string ToPluralString(this int count, CultureInfo culture, string one, string few, string many)
    {
        var format = culture.TwoLetterISOLanguageName == "ru"
            ? GetRussianForm(count, one, few, many)
            : count == 1 ? one : many;

        return format.Fill(count);
    }

    private static string GetRussianForm(int count, string one, string few, string many)
    {
        var lastTwoDigits = Math.Abs(count) % 100;
        var lastDigit = lastTwoDigits % 10;

        if (lastTwoDigits is >= 11 and <= 14)
            return many;

        if (lastDigit == 1)
            return one;

        if (lastDigit is >= 2 and <= 4)
            return few;

        return many;
    }
}
