using CommunityToolkit.Maui.Extensions;
using TrainingDay.Maui.ViewModels;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Controls.PopUp;

public partial class MoveOrCopyExercisesPopup : ContentView
{
    public MoveOrCopyExercisesPopup()
    {
        InitializeComponent();
    }

    private TrainingExercisesPageViewModel Context => BindingContext as TrainingExercisesPageViewModel;

    private void Training_Tapped(object sender, TappedEventArgs e)
    {
        Context.SelectedTrainingForCopyOrMove = e.Parameter as TrainingViewModel;
        Context.AcceptTrainingForMoveOrCopyCommand.Execute(null);
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.ClosePopupAsync();
    }
}
