namespace TrainingDay.Maui.Models.Questions
{
	public record WorkoutQuestinariumStep(string Title, bool IsMultiple, int Number, IEnumerable<string> Answers);
}

