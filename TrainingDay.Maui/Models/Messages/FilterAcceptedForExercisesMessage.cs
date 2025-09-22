namespace TrainingDay.Maui.Models.Messages
{
    public class FilterAcceptedForExercisesMessage(FilterModel filter)
    {
        public FilterModel Filter { get; set; } = filter;
    }
}
