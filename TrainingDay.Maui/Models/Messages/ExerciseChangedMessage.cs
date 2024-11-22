using TrainingDay.Maui.Models.Database;

namespace TrainingDay.Maui.Models.Messages;

public class ExerciseChangedMessage
{
    public enum ExerciseAction
    {
        Added = 0,
        Removed,
        Changed,
    }

    public Exercise Sender { get; set; }

    public ExerciseAction Action { get; set; }

    public ExerciseChangedMessage(Exercise sender, ExerciseAction action)
    {
        Sender = sender;
        Action = action;
    }
}