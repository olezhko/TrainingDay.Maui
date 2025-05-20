using TrainingDay.Common;
using TrainingDay.Common.Models;

namespace TrainingDay.Maui.ViewModels;

public class MuscleCheckItem : BaseViewModel
{
    private bool isChecked;

    public bool IsChecked
    {
        get => isChecked;
        set
        {
            isChecked = value;
            OnPropertyChanged();
        }
    }

    public string Text { get; set; }

    public MusclesEnum Muscle { get; set; }
}