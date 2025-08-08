using Newtonsoft.Json;
using System.Collections.ObjectModel;
using TrainingDay.Common.Communication;
using TrainingDay.Common.Extensions;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Services
{
    public interface IWorkoutService
    {
        Task CreateWorkoutAsync(string name, IEnumerable<ExerciseQueryResponse> exercises, CancellationToken token = default);
        Task UpdateExerciseNameAndDescription();

	}

    public class WorkoutService : IWorkoutService
    {
        public async Task UpdateExerciseNameAndDescription()
        {
            var inits = await ResourceExtension.LoadResource<BaseExercise>("exercises", Settings.GetLanguage().TwoLetterISOLanguageName);
            var exers = App.Database.GetExerciseItems();
            exers.Where(item => item.CodeNum != 0).ForEach(exer =>
            {
                try
                {
                    var init = inits.FirstOrDefault(item => item.CodeNum == exer.CodeNum);
                    if (init != null)
                    {
                        exer.Description = JsonConvert.SerializeObject(init.Description);
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

        public async Task CreateWorkoutAsync(string name, IEnumerable<ExerciseQueryResponse> exercises, CancellationToken token = default)
        {
            var id = App.Database.SaveItem(new TrainingDto()
            {
                Title = name,
            });

            var baseExercises = App.Database.GetExerciseItems();
            int index = 0;
            foreach (var exercise in exercises)
            {
                var exerciseItem = baseExercises.FirstOrDefault(item => item.CodeNum == exercise.Guid);
                var trainingExercise = new TrainingExerciseDto()
                {
                    TrainingId = id,
                    ExerciseId = exerciseItem.Id,
                    OrderNumber = index,
                    WeightAndRepsString = CreateWeightAndReps(exerciseItem.TagsValue, exercise)
                };

                App.Database.SaveItem(trainingExercise);

                index++;
            }

            return;
        }

        private string CreateWeightAndReps(int tagsValue, ExerciseQueryResponse exercise)
        {
            var exerciseModel = new TrainingExerciseViewModel();
            var tags = ExerciseExtensions.ConvertTagIntToList(tagsValue);
            try
            {
                if (tags.Contains(ExerciseTags.ExerciseByReps) || tags.Contains(ExerciseTags.ExerciseByRepsAndWeight))
                {
                    exerciseModel.WeightAndRepsItems = new ObservableCollection<WeightAndRepsViewModel>();
                    for (int i = 0; i < exercise.CountOfSets; i++)
                    {
                        exerciseModel.WeightAndRepsItems.Add(new WeightAndRepsViewModel(exercise.WorkingWeight, Convert.ToInt32(exercise.CountOfRepsOrTime)));
                    }
                }

                if (tags.Contains(ExerciseTags.ExerciseByTime))
                {
                    exerciseModel.Time = TimeSpan.Parse(exercise.CountOfRepsOrTime);
                }
            }
            catch (Exception)
            {

            }

            return ExerciseManager.ConvertJson(tags, exerciseModel);
        }
    }
}
