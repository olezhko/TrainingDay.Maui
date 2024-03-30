using CommunityToolkit.Mvvm.Messaging;
using TrainingDay.Maui.Models.Messages;

namespace TrainingDay.Maui.Views;

public partial class TrainingSettingsPage : ContentPage
{
    public enum TrainingSettingsActions
    {
        AddAlarm,
        ShareTraining,
        SuperSetAction,
        MoveExercises,
        CopyExercises,
    }

    public TrainingSettingsPage()
    {
        InitializeComponent();
    }

    private async void AddAlarmCommand_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
        WeakReferenceMessenger.Default.Send(new TrainingSettingsActionMessage(TrainingSettingsActions.AddAlarm));
    }

    private async void ShareTrainingCommand_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
        WeakReferenceMessenger.Default.Send(new TrainingSettingsActionMessage(TrainingSettingsActions.ShareTraining));
    }

    private async void SetSuperSetCommand_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
        WeakReferenceMessenger.Default.Send(new TrainingSettingsActionMessage(TrainingSettingsActions.SuperSetAction));
    }

    private async void StartMoveExerciseCommand_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
        WeakReferenceMessenger.Default.Send(new TrainingSettingsActionMessage(TrainingSettingsActions.MoveExercises));
    }

    private async void StartCopyExerciseCommand_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
        WeakReferenceMessenger.Default.Send(new TrainingSettingsActionMessage(TrainingSettingsActions.CopyExercises));
    }
}