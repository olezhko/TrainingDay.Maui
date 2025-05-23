﻿using System.Globalization;
using TrainingDay.Common.Models;

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
        var tagChecked = (ExerciseTags)parameter;
        var tags = value as List<ExerciseTags> ?? new List<ExerciseTags>();

        if ((bool)value)
        {
            if (!tags.Contains(tagChecked))
            {
                tags.Add(tagChecked);
            }
        }
        else
        {
            if (tags.Contains(tagChecked))
            {
                tags.Remove(tagChecked);
            }
        }

        return tags;
    }
}