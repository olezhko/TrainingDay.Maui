using Newtonsoft.Json;
using System.Collections.ObjectModel;
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
            weightAndReps = JsonConvert.SerializeObject(viewmodel.WeightAndRepsItems);
        }

        if (tagsList.Contains(ExerciseTags.ExerciseByTime) || tagsList.Contains(ExerciseTags.ExerciseByDistance))
        {
            weightAndReps = JsonConvert.SerializeObject((viewmodel.Time, viewmodel.Distance));
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
                viewmodel.WeightAndRepsItems = JsonConvert.DeserializeObject<ObservableCollection<WeightAndRepsViewModel>>(value);
            }

            if (tagsList.Contains(ExerciseTags.ExerciseByTime) || tagsList.Contains(ExerciseTags.ExerciseByDistance))
            {
                var obj = JsonConvert.DeserializeObject<(TimeSpan, double)>(value);
                viewmodel.Distance = obj.Item2;
                viewmodel.Time = obj.Item1;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}