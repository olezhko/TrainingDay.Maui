using TrainingDay.Maui.Models.Database;

namespace TrainingDay.Maui.Models;

public class ExerciseChangedEventArgs
{
    public enum ExerciseAction
    {
        Added = 0,
        Removed,
        Changed,
    }

    public Exercise Sender { get; set; }

    public ExerciseAction Action { get; set; }

    public ExerciseChangedEventArgs(Exercise sender, ExerciseAction action)
    {
        Sender = sender;
        Action = action;
    }
}