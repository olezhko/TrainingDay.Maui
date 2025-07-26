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

    public TrainingExercisesPageViewModel Context
    {
        get => BindingContext as TrainingExercisesPageViewModel;
        set => BindingContext = value;
    }

    void GroupCollection_SelectionChanged(System.Object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {
        Context.SelectedTrainingForCopyOrMove = GroupCollection.SelectedItem as TrainingViewModel;
        Context.AcceptTrainingForMoveOrCopyCommand.Execute(null);
    }
}