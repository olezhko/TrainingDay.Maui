using System.Collections.ObjectModel;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Mapper
{
    public static class MapBaseSuperSetToImplementMapper
    {
        public static ImplementTrainingExerciseViewModel Map(TrainingExerciseViewModel exerciseItem)
        {
            return new ImplementTrainingExerciseViewModel()
            {
                CodeNum = exerciseItem.CodeNum,
                Description = exerciseItem.Description,
                ExerciseId = exerciseItem.ExerciseId,
                TrainingId = exerciseItem.TrainingId,
                DifficultType = exerciseItem.DifficultType,
                Muscles = exerciseItem.Muscles,
                Name = exerciseItem.Name,
                OrderNumber = exerciseItem.OrderNumber,
                SuperSetId = exerciseItem.SuperSetId,
                SuperSetNum = exerciseItem.SuperSetNum,
                Tags = exerciseItem.Tags,
                Time = exerciseItem.Time,
                TimeString = exerciseItem.TimeString,
                WeightAndRepsItems = exerciseItem.WeightAndRepsItems,
                Id = exerciseItem.Id,
                ImageData = exerciseItem.ImageData,
                IsNotFinished = true,
                TrainingExerciseId = exerciseItem.TrainingExerciseId,
            };
        }

        internal static SuperSetViewModel<ImplementTrainingExerciseViewModel> Map(SuperSetViewModel<TrainingExerciseViewModel> item)
        {
            var result = new SuperSetViewModel<ImplementTrainingExerciseViewModel>()
            {
                Id = item.Id,
                TrainingId = item.TrainingId,
            };

            foreach (var exerciseItem in item.SuperSetItems)
            {
                var mappedExercise = Map(exerciseItem);
                result.Add(mappedExercise);
            }

            return result;
        }
    }
}
