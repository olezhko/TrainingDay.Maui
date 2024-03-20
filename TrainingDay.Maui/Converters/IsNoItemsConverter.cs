using System.Collections.ObjectModel;
using System.Globalization;

namespace TrainingDay.Maui.Converters;

public class IsNoItemsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ObservableCollection<object> items)
        {
            return items.Count > 0;
        }

        if (!(value is int))
        {
            return false;
        }

        var count = (int)value;
        return count > 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}