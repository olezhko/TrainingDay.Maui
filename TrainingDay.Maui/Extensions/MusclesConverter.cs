using System.Text;
using TrainingDay.Common.Models;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Extensions;

public class MusclesConverter
{
    public static List<MuscleViewModel> SetMuscles(params MusclesEnum[] muscles)
    {
        List<MuscleViewModel> result = new List<MuscleViewModel>();
        foreach (var type in muscles)
        {
            var muscle = new MuscleViewModel(type);
            if (result.All(item => item.Id != muscle.Id))
            {
                result.Add(muscle);
            }
        }

        return result;
    }

    public static List<MuscleViewModel> ConvertFromStringToList(string value) // separator
    {
        List<MuscleViewModel> muscle = new List<MuscleViewModel>();

        if (!string.IsNullOrEmpty(value))
        {
            string[] enums = value.Split(',');
            try
            {
                foreach (var type in enums)
                {
                    var res = Enum.TryParse(type, out MusclesEnum muscleValue);
                    if (res)
                    {
                        muscle.Add(new MuscleViewModel(muscleValue));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        return muscle;
    }

    public static string ConvertFromListToString(List<MuscleViewModel> array)
    {
        try
        {
            if (array.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder res = new StringBuilder();
            foreach (var muscleViewModel in array)
            {
                res.Append((MusclesEnum)muscleViewModel.Id);
                res.Append(",");
            }

            res.Remove(res.Length - 1, 1);
            return res.ToString();
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}