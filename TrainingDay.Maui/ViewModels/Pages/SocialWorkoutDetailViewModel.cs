using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Common.Extensions;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using ExerciseEntity = TrainingDay.Maui.Models.Database.ExerciseEntity;
using Training = TrainingDay.Maui.Models.Database.TrainingEntity;
using TrainingExerciseComm = TrainingDay.Maui.Models.Database.TrainingExerciseEntity;

namespace TrainingDay.Maui.ViewModels.Pages;

public class SocialWorkoutDetailViewModel : BaseViewModel
{
    private readonly ISocialWorkoutsService socialWorkoutsService;

    public SocialWorkoutDetailViewModel(SocialWorkoutViewModel workout, ISocialWorkoutsService socialWorkoutsService)
    {
        Workout = workout;
        this.socialWorkoutsService = socialWorkoutsService;
        Exercises = new ObservableCollection<TrainingExerciseViewModel>(workout.Exercises.Select(CreateExerciseViewModel));
    }

    public SocialWorkoutViewModel Workout { get; }

    public ObservableCollection<TrainingExerciseViewModel> Exercises { get; }

    public ICommand AddToMyWorkoutsCommand => new AsyncRelayCommand(AddToMyWorkouts);

    public ICommand LikeCommand => new AsyncRelayCommand(ToggleLike);

    private static TrainingExerciseViewModel CreateExerciseViewModel(SocialWorkoutExerciseDisplay dto)
    {
        var vm = new TrainingExerciseViewModel
        {
            Name = dto.ExerciseName,
            Muscles = new ObservableCollection<MuscleViewModel>(MusclesExtensions.ConvertFromStringToList(string.Join(",", dto.MusclesString))),
            Tags = [.. dto.TagsValue.Select(Enum.Parse<ExerciseTags>)],
            CodeNum = dto.CodeNum,
        };

        ExerciseManager.ConvertJsonBack(vm, dto.WeightAndRepsString);

        return vm;
    }

    private async Task AddToMyWorkouts()
    {
        try
        {
            var localExercises = App.Database.GetExerciseItems().ToList();

            var trainingId = App.Database.SaveTrainingItem(new Training
            {
                Title = Workout.Name,
            });

            int order = 0;
            foreach (var exercise in Workout.Exercises)
            {
                var localExercise = exercise.CodeNum != 0
                    ? localExercises.FirstOrDefault(item => item.CodeNum == exercise.CodeNum)
                    : null;

                if (localExercise == null)
                {
                    var newId = App.Database.SaveExerciseItem(new ExerciseEntity
                    {
                        Name = exercise.ExerciseName,
                        MusclesString = string.Join(",", exercise.MusclesString),
                        TagsValue = ExerciseExtensions.ConvertTagListToInt(exercise.TagsValue.Select(Enum.Parse<ExerciseTags>)),
                        CodeNum = exercise.CodeNum,
                    });
                    localExercise = App.Database.GetExerciseItem(newId);
                    localExercises.Add(localExercise);
                }

                App.Database.SaveTrainingExerciseItem(new TrainingExerciseComm
                {
                    ExerciseId = localExercise.Id,
                    TrainingId = trainingId,
                    OrderNumber = order,
                    WeightAndRepsString = exercise.WeightAndRepsString,
                });

                order++;
            }

            await Toast.Make(AppResources.WorkoutAddedSuccessfully).Show();
            LoggingService.TrackEvent("Workout Imported");

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            LoggingService.TrackError(ex);
        }
    }

    private async Task ToggleLike()
    {
        try
        {
            if (Workout.IsLikedByMe)
            {
                var result = await socialWorkoutsService.UnlikeAsync(Workout.ServerId);
                if (result)
                {
                    Workout.IsLikedByMe = false;
                    Workout.LikesCount--;
                    LoggingService.TrackEvent("Workout Unliked");
                }
            }
            else
            {
                var result = await socialWorkoutsService.LikeAsync(Workout.ServerId);
                if (result)
                {
                    Workout.IsLikedByMe = true;
                    Workout.LikesCount++;
                    LoggingService.TrackEvent("Workout Liked");
                }
            }
        }
        catch (Exception ex)
        {
            LoggingService.TrackError(ex);
        }
    }
}
