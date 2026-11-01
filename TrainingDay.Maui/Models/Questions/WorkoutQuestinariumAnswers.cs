namespace TrainingDay.Maui.Models.Questions;

/// <summary>Typed answers of the workout questionnaire.</summary>
public sealed record WorkoutQuestinariumAnswers(
    WorkoutGender Gender,
    WorkoutLevel Level,
    IReadOnlyCollection<WorkoutGoal> Goals,
    IReadOnlyCollection<WorkoutEquipment> Equipment,
    IReadOnlyCollection<WorkoutTechnique> Techniques,
    IReadOnlyCollection<WorkoutMuscleGroup> Muscles,
    WorkoutDuration Duration)
{
    /// <summary>Builds answers from the selected <c>option</c> values of each question; unknown options are ignored.</summary>
    public static WorkoutQuestinariumAnswers Parse(IReadOnlyDictionary<WorkoutQuestion, IReadOnlyCollection<string>> options)
    {
        return new WorkoutQuestinariumAnswers(
            ParseMany<WorkoutGender>(options, WorkoutQuestion.Gender).DefaultIfEmpty(WorkoutGender.Male).First(),
            ParseMany<WorkoutLevel>(options, WorkoutQuestion.Level).DefaultIfEmpty(WorkoutLevel.Beginner).First(),
            ParseMany<WorkoutGoal>(options, WorkoutQuestion.Goals),
            ParseMany<WorkoutEquipment>(options, WorkoutQuestion.Equipment),
            ParseMany<WorkoutTechnique>(options, WorkoutQuestion.Techniques),
            ParseMany<WorkoutMuscleGroup>(options, WorkoutQuestion.Muscles),
            ParseMany<WorkoutDuration>(options, WorkoutQuestion.Duration).DefaultIfEmpty(WorkoutDuration.Minutes60).First());
    }

    private static List<T> ParseMany<T>(IReadOnlyDictionary<WorkoutQuestion, IReadOnlyCollection<string>> options, WorkoutQuestion question)
        where T : struct, Enum
    {
        if (!options.TryGetValue(question, out var values))
        {
            return [];
        }

        return values
            .Select(value => Enum.TryParse<T>(value, true, out var result) && Enum.IsDefined(result) ? (T?)result : null)
            .OfType<T>()
            .Distinct()
            .ToList();
    }
}
