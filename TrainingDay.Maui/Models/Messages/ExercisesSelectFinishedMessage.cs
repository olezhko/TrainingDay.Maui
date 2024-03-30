using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Models.Messages
{
    internal class ExercisesSelectFinishedMessage
    {
        public List<ExerciseListItemViewModel> Selected;
        public ExercisesSelectFinishedMessage(List<ExerciseListItemViewModel> selected)
        {
            Selected = selected;
        }
    }
}
