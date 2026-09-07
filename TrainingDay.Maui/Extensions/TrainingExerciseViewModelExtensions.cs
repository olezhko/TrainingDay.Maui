using System.Collections.ObjectModel;
using System.Text.Json;
using TrainingDay.Common.Models;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Extensions;

public class TrainingExerciseViewModelExtensions
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
            weightAndReps = JsonSerializer.Serialize(viewmodel.Time);
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
                viewmodel.Time = JsonSerializer.Deserialize<TimeSpan>(value);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}