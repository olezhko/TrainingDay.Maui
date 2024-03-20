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

    public event EventHandler<TrainingSettingsActions> ActionSelected;
    private async void AddAlarmCommand_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
        ActionSelected?.Invoke(this, TrainingSettingsActions.AddAlarm);
    }

    private async void ShareTrainingCommand_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
        ActionSelected?.Invoke(this, TrainingSettingsActions.ShareTraining);
    }

    private async void SetSuperSetCommand_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
        ActionSelected?.Invoke(this, TrainingSettingsActions.SuperSetAction);
    }

    private async void StartMoveExerciseCommand_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
        ActionSelected?.Invoke(this, TrainingSettingsActions.MoveExercises);
    }

    private async void StartCopyExerciseCommand_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
        ActionSelected?.Invoke(this, TrainingSettingsActions.CopyExercises);
    }
}