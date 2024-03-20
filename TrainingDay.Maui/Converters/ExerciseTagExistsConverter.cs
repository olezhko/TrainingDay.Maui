using System.Globalization;
using TrainingDay.Common;

namespace TrainingDay.Maui.Converters;

public class ExerciseTagExistsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var tagChecked = (ExerciseTags)parameter;
        var tags = value as List<ExerciseTags>;
        if (tags == null)
        {
            return false;
        }

        return tags.Contains(tagChecked);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return 0;
    }
}