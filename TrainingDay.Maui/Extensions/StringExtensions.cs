namespace TrainingDay.Maui.Extensions;

public static class StringExtensions
{
    public static bool Contains(this string str, string substring, StringComparison comp)
    {
        if (substring == null)
            throw new ArgumentNullException("substring", "substring cannot be null.");
        else if (!Enum.IsDefined(typeof(StringComparison), comp))
            throw new ArgumentException("comp is not a member of StringComparison", "comp");

        return str.IndexOf(substring, comp) >= 0;
    }

    public static bool IsNotNullOrEmpty(this string str)
    {
        return !string.IsNullOrEmpty(str);
    }

    public static bool IsNullOrEmptyOrWhiteSpace(this string str)
    {
        return string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str);
    }

    public static string Fill(this string str, params object[] args)
    {
        return string.Format(str, args);
    }

    public static string FirstCharToUpper(this string input)
    {
        if (String.IsNullOrEmpty(input))
            throw new ArgumentException("FirstCharToUpper");

        return input.First().ToString().ToUpper() + string.Join("", input.Skip(1));
    }
}