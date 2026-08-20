using TrainingDay.Common.Extensions;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Services;
using ExerciseEntity = TrainingDay.Maui.Models.Database.ExerciseEntity;

namespace TrainingDay.Maui.ViewModels;

public class SocialWorkoutViewModel : BaseViewModel
{
    public SocialWorkoutViewModel(SocialWorkoutDto dto, IReadOnlyList<ExerciseEntity> localExercises)
    {
        ServerId = dto.Id;
        OwnerNickname = dto.OwnerNickname;
        Name = dto.WorkoutName;
        Date = dto.Date;
        Duration = dto.Duration;
        Exercises = MergeExercises(dto, localExercises);
        LikesCount = dto.LikesCount;
        IsLikedByMe = dto.LikedByMe;

        TopMuscles = string.Join(", ", Exercises
            .SelectMany(item => MuscleViewModelExtensions.ConvertFromStringToList(string.Join(",", item.MusclesString)))
            .GroupBy(muscle => muscle.Id)
            .OrderByDescending(group => group.Count())
            .Take(3)
            .Select(group => group.First().Name));
    }

    /// <summary>
    /// The server only stores CodeNum + set data for base-library exercises (to avoid duplicating
    /// exercise content it already owns); resolve name/muscles/tags from the local exercise DB.
    /// Custom exercises already carry their own name/muscles/tags from the server.
    /// </summary>
    private static List<SocialWorkoutExerciseDisplay> MergeExercises(SocialWorkoutDto dto, IReadOnlyList<ExerciseEntity> localExercises)
    {
        var result = new List<SocialWorkoutExerciseDisplay>();

        foreach (var baseExercise in dto.BaseExercises)
        {
            var local = localExercises?.FirstOrDefault(item => item.CodeNum == baseExercise.CodeNum);
            result.Add(new SocialWorkoutExerciseDisplay
            {
                ExerciseName = local?.Name ?? string.Empty,
                MusclesString = local?.MusclesString != null ? [.. local.MusclesString.Split(',', StringSplitOptions.RemoveEmptyEntries)] : [],
                WeightAndRepsString = baseExercise.WeightAndRepsString,
                TagsValue = local != null ? [.. ExerciseExtensions.ConvertTagIntToList(local.TagsValue).Select(tag => tag.ToString())] : [],
                CodeNum = baseExercise.CodeNum,
            });
        }

        result.AddRange(dto.CustomExercises.Select(item => new SocialWorkoutExerciseDisplay
        {
            ExerciseName = item.ExerciseName,
            MusclesString = [.. item.MusclesString],
            WeightAndRepsString = item.WeightAndRepsString,
            TagsValue = [.. item.TagsValue],
            CodeNum = 0,
        }));

        return result;
    }

    private string serverId;
    public string ServerId
    {
        get => serverId;
        set => SetProperty(ref serverId, value);
    }

    private string ownerNickname;
    public string OwnerNickname
    {
        get => ownerNickname;
        set => SetProperty(ref ownerNickname, value);
    }

    private string name;
    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    private DateTime date;
    public DateTime Date
    {
        get => date;
        set
        {
            if (SetProperty(ref date, value))
            {
                OnPropertyChanged(nameof(DateFormatted));
            }
        }
    }

    private TimeSpan duration;
    public TimeSpan Duration
    {
        get => duration;
        set
        {
            if (SetProperty(ref duration, value))
            {
                OnPropertyChanged(nameof(DurationFormatted));
            }
        }
    }

    public string DurationFormatted => Duration.ToString(@"hh\:mm\:ss");

    public string DateFormatted => Date.ToString("d", Settings.GetLanguage());

    private string topMuscles;
    public string TopMuscles
    {
        get => topMuscles;
        set => SetProperty(ref topMuscles, value);
    }

    private List<SocialWorkoutExerciseDisplay> exercises;
    public List<SocialWorkoutExerciseDisplay> Exercises
    {
        get => exercises;
        set
        {
            if (SetProperty(ref exercises, value))
            {
                OnPropertyChanged(nameof(ExerciseCount));
            }
        }
    }

    public int ExerciseCount => Exercises?.Count ?? 0;

    private int likesCount;
    public int LikesCount
    {
        get => likesCount;
        set => SetProperty(ref likesCount, value);
    }

    private bool isLikedByMe;
    public bool IsLikedByMe
    {
        get => isLikedByMe;
        set => SetProperty(ref isLikedByMe, value);
    }
}
