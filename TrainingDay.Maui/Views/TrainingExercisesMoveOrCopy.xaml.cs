using TrainingDay.Maui.ViewModels;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

[QueryProperty(nameof(Context), "Context")]
public partial class TrainingExercisesMoveOrCopy : ContentPage
{
    public TrainingExercisesMoveOrCopy()
    {
        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        Context.IsExercisesCheckBoxVisible = false;
        Context.CurrentAction = ExerciseCheckBoxAction.None;
        base.OnDisappearing();
    }

    public TrainingExercisesPageViewModel Context
    {
        get => BindingContext as TrainingExercisesPageViewModel;
        set => BindingContext = value;
    }

    void GroupCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Context.SelectedTrainingForCopyOrMove = GroupCollection.SelectedItem as TrainingViewModel;
        Context.AcceptTrainingForMoveOrCopyCommand.Execute(null);
    }
}