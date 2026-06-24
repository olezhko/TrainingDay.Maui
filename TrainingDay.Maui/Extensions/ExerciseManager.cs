using System.Collections.ObjectModel;
using System.Text.Json;
using TrainingDay.Common.Models;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Extensions;

public class ExerciseManager
{
    public static string ConvertJson(IEnumerable<ExerciseTags> tagsList, TrainingExerciseViewModel viewmodel)
    {
        string weightAndReps = default;
        if (tagsList.Contains(ExerciseTags.ExerciseByRepsAndWeight) || tagsList.Contains(ExerciseTags.ExerciseByReps))
        {
            weightAndReps = JsonSerializer.Serialize(viewmodel.WeightAndRepsItems);
        }

        if (tagsList.Contains(ExerciseTags.ExerciseByTime))
        {
            weightAndReps = JsonSerializer.Serialize((viewmodel.Time, 0));
        }

        return weightAndReps;
    }

    public static void ConvertJsonBack(TrainingExerciseViewModel viewmodel, string value)
    {
        try
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value) || value.Length < 0)
            {
                return;
            }

            var tagsList = viewmodel.Tags;
            if (tagsList.Contains(ExerciseTags.ExerciseByRepsAndWeight) || tagsList.Contains(ExerciseTags.ExerciseByReps))
            {
                viewmodel.WeightAndRepsItems = JsonSerializer.Deserialize<ObservableCollection<WeightAndRepsViewModel>>(value);
            }

            if (tagsList.Contains(ExerciseTags.ExerciseByTime))
            {
                var obj = JsonSerializer.Deserialize<(TimeSpan, double)>(value);
                viewmodel.Time = obj.Item1;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}