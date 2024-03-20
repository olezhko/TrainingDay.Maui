using System.Globalization;

namespace TrainingDay.Maui.Converters;

public class HalfValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double val = (double)value;
        int offset = int.Parse((string)parameter);
        if (Math.Abs(val - (-1)) < 0.00001)
        {
            return 0;
        }

        return (((double)value) / offset) - 4;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}