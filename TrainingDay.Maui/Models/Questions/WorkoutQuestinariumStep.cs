namespace TrainingDay.Maui.Models.Questions;

public sealed record WorkoutQuestinariumStep(
    string Title,
    string Instruction,
    bool IsMultiple,
    int Number,
    IEnumerable<Variant> Answers);

public sealed record Variant(string Answer, string Option);