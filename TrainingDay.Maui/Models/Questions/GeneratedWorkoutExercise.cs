using TrainingDay.Maui.Models.Database;

namespace TrainingDay.Maui.Models.Questions;

/// <summary>One exercise of a workout built from the questionnaire.</summary>
/// <param name="SuperSetNumber">Exercises sharing the same non-zero number form one superset; 0 means no superset.</param>
public sealed record GeneratedWorkoutExercise(
    ExerciseEntity Exercise,
    string WeightAndRepsString,
    int SuperSetNumber);
