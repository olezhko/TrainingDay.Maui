namespace TrainingDay.Maui.Models.Messages
{
    public class FilterAcceptedForExercisesMessage
    {
        public FilterAcceptedForExercisesMessage(FilterModel filter)
        {
            Filter = filter;
        }

        public FilterModel Filter { get; set; }
    }
}
