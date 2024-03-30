using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

[QueryProperty(nameof(Context), "Context")]
public partial class TrainingExercisesMoveOrCopy : ContentPage
{
    public TrainingExercisesMoveOrCopy()
    {
        InitializeComponent();
    }

    public TrainingExercisesPageViewModel Context
    {
        get => BindingContext as TrainingExercisesPageViewModel;
        set => BindingContext = value;
    }
}