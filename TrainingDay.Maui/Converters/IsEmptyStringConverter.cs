using System.Globalization;

namespace TrainingDay.Maui.Converters;

public class IsEmptyStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string str = value as string;
        var result = string.IsNullOrEmpty(str);
        if (parameter != null && parameter.ToString() == "inverse")
        {
            return !result;
        }
        return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}