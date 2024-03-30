using static TrainingDay.Maui.Views.TrainingSettingsPage;

namespace TrainingDay.Maui.Models.Messages;

internal class TrainingSettingsActionMessage
{
    public TrainingSettingsActionMessage(TrainingSettingsActions action)
    {
        Action = action;
    }

    public TrainingSettingsActions Action { get; }
}