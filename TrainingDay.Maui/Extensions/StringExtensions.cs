namespace TrainingDay.Maui.Extensions;

public static class StringExtensions
{
    public static bool IsNotNullOrEmpty(this string str)
    {
        return !string.IsNullOrEmpty(str);
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