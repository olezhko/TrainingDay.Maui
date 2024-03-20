namespace TrainingDay.Maui.ViewModels;

public class MuscleLookupItem : BaseViewModel
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

    public MuscleViewModel Muscle { get; set; }
}