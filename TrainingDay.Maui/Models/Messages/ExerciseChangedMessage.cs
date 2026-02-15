using TrainingDay.Maui.Models.Database;

namespace TrainingDay.Maui.Models.Messages;

public class ExerciseChangedMessage(ExerciseEntity sender, ExerciseChangedMessage.ExerciseAction action)
{
    public enum ExerciseAction
    {
        Added = 0,
        Removed,
        Changed,
    }

    public ExerciseEntity Sender { get; set; } = sender;

    public ExerciseAction Action { get; set; } = action;
}