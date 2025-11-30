using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Models.Messages
{
    internal class ExercisesSelectFinishedMessage(List<ExerciseListItemViewModel> selected)
    {
        public List<ExerciseListItemViewModel> Selected = selected;
    }
}
