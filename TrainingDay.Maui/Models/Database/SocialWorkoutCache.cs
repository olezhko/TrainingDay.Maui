using SQLite;
using System.Text.Json;
using TrainingDay.Common.Models;

namespace TrainingDay.Maui.Models.Database;

/// <summary>
/// Local offline cache of the last 10 social-workout feed items, used as a fallback when the
/// device has no connectivity.
/// </summary>
[Table("SocialWorkoutsCache")]
public class SocialWorkoutCacheEntity
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public int Id { get; set; }

    public string ServerId { get; set; }

    public string OwnerUserId { get; set; }

    public string OwnerNickname { get; set; }

    public string WorkoutName { get; set; }

    public DateTime Date { get; set; }

    public long DurationTicks { get; set; }

    public string BaseExercisesJson { get; set; }

    public string CustomExercisesJson { get; set; }

    public int LikesCount { get; set; }

    public bool LikedByMe { get; set; }

    public static SocialWorkoutCacheEntity FromDto(SocialWorkoutDto dto)
    {
        return new SocialWorkoutCacheEntity
        {
            ServerId = dto.Id,
            OwnerUserId = dto.OwnerUserId,
            OwnerNickname = dto.OwnerNickname,
            WorkoutName = dto.WorkoutName,
            Date = dto.Date,
            DurationTicks = dto.Duration.Ticks,
            BaseExercisesJson = JsonSerializer.Serialize(dto.BaseExercises),
            CustomExercisesJson = JsonSerializer.Serialize(dto.CustomExercises),
            LikesCount = dto.LikesCount,
            LikedByMe = dto.LikedByMe,
        };
    }

    public SocialWorkoutDto ToDto()
    {
        return new SocialWorkoutDto
        {
            Id = ServerId,
            OwnerUserId = OwnerUserId,
            OwnerNickname = OwnerNickname,
            WorkoutName = WorkoutName,
            Date = Date,
            Duration = TimeSpan.FromTicks(DurationTicks),
            BaseExercises = JsonSerializer.Deserialize<List<SocialWorkoutBaseExerciseDto>>(BaseExercisesJson) ?? [],
            CustomExercises = JsonSerializer.Deserialize<List<SocialWorkoutExerciseDto>>(CustomExercisesJson) ?? [],
            LikesCount = LikesCount,
            LikedByMe = LikedByMe,
        };
    }
}
