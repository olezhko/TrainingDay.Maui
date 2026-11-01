using System.Text.Json;
using TrainingDay.Common.Communication;
using TrainingDay.Common.Extensions;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Mappers;
using TrainingDay.Maui.Models.Database;

namespace TrainingDay.Maui.Services
{
    public interface IWorkoutService
    {
        Task CreateWorkoutAsync(string name, IEnumerable<ExerciseQueryResponse> exercises, CancellationToken token);
        Task UpdateExerciseNameAndDescription();
	}

    public class WorkoutService : IWorkoutService
    {
        public async Task UpdateExerciseNameAndDescription()
        {
            var inits = await ResourceExtension.LoadResourceAsync<BaseExercise>("exercises", Settings.GetLanguage().TwoLetterISOLanguageName);
            var exers = App.Database.GetExerciseItems();
            exers.Where(item => item.CodeNum != 0).ForEach(exer =>
            {
                try
                {
                    var init = inits.FirstOrDefault(item => item.CodeNum == exer.CodeNum);
                    if (init != null)
                    {
                        exer.Description = JsonSerializer.Serialize(init.Description);
                        exer.Name = init.Name;
                        App.Database.SaveExerciseItem(exer);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
        }

        public async Task CreateWorkoutAsync(string name, IEnumerable<ExerciseQueryResponse> exercises, CancellationToken token)
        {
            var id = App.Database.SaveItem(new TrainingEntity()
            {
                Title = name,
            });

            var baseExercises = App.Database.GetExerciseItems();
            int index = 0;
            foreach (var exercise in exercises)
            {
                var exerciseItem = baseExercises.FirstOrDefault(item => item.CodeNum == exercise.Guid);
                var trainingExercise = new TrainingExerciseEntity()
                {
                    TrainingId = id,
                    ExerciseId = exerciseItem.Id,
                    OrderNumber = index,
                    WeightAndRepsString = TrainingExerciseMapper.ToWeightAndRepsString(exerciseItem.TagsValue, exercise)
                };

                App.Database.SaveItem(trainingExercise);

                index++;
            }

            return;
        }
    }
}
