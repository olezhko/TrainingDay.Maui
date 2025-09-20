namespace TrainingDay.Maui.Models.Questions
{
	public record WorkoutQuestinariumStep(
		string Title,
		string Instruction,
		bool IsMultiple,
		int Number,
		IEnumerable<Variant> Answers);
}

public record Variant(string Answer, string Option);

