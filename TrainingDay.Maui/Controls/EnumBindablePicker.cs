using TrainingDay.Common;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.Controls;

public class EnumBindablePicker<T> : Picker where T : struct
{
    public EnumBindablePicker()
    {
        SelectedIndexChanged += OnSelectedIndexChanged;
        foreach (var value in Enum.GetValues(typeof(T)))
        {
            Items.Add(ExerciseTools.GetEnumDescription(value, Settings.GetLanguage()));
        }
    }

    private void OnSelectedIndexChanged(object sender, EventArgs eventArgs)
    {
        if (SelectedIndex < 0 || SelectedIndex > Items.Count - 1)
        {
            //SelectedItem = default(T);
        }
        else
        {
            //try parsing, if using description this will fail
            T match;
            if (!Enum.TryParse<T>(Items[SelectedIndex], out match))
            {
                //find enum by Description
                match = GetEnumByDescription(Items[SelectedIndex]);
            }

            SelectedItem = (T)Enum.Parse(typeof(T), match.ToString());
        }
    }

    private T GetEnumByDescription(string description)
    {
        return Enum.GetValues(typeof(T)).Cast<T>().FirstOrDefault(x =>
            string.Equals(ExerciseTools.GetEnumDescription(x, Settings.GetLanguage()), description));
    }
}