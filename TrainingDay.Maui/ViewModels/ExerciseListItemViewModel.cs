using TrainingDay.Maui.Models.Database;

namespace TrainingDay.Maui.ViewModels;

public class ExerciseListItemViewModel : ExerciseViewModel
{
    private bool isSelected;
    private bool isExerciseExistsInWorkout;
    public bool IsExerciseExistsInWorkout
    {
        get => isExerciseExistsInWorkout;
        set => SetProperty(ref isExerciseExistsInWorkout, value);
    }

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            isSelected = value;
            OnPropertyChanged();
        }
    }

    public ExerciseListItemViewModel(ExerciseDto exercise) : base(exercise)
    {
        Id = exercise.Id;
    }
}