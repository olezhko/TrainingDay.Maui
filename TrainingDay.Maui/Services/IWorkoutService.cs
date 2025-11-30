using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
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
                        var isValid = TryGetReps(exercise.CountOfRepsOrTime, out var result);
                        exerciseModel.WeightAndRepsItems.Add(new WeightAndRepsViewModel(exercise.WorkingWeight, isValid ? result : 5));
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

        public static bool TryGetReps(string input, out int number1)
        {
            number1 = 0;
            var number2 = 0;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            // Pattern 1: "{number} {text}" (e.g., "123 abc")
            var pattern1 = @"^\s*(\d+)\s+([a-zA-Z]+)\s*$";
            
            // Pattern 2: "{number}-{number}" (e.g., "123-456")
            var pattern2 = @"^\s*(\d+)\s*-\s*(\d+)\s*$";

            var match1 = Regex.Match(input, pattern1);
            if (match1.Success)
            {
                if (int.TryParse(match1.Groups[1].Value, out number1))
                {
                    var text = match1.Groups[2].Value;
                    return true;
                }
            }

            var match2 = Regex.Match(input, pattern2);
            if (match2.Success)
            {
                if (int.TryParse(match2.Groups[1].Value, out number1) && 
                    int.TryParse(match2.Groups[2].Value, out number2))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
