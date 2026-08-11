namespace TrainingDay.Maui.Models;

/// <summary>
/// Client-only display shape merging a SocialWorkoutDto's BaseExercises (resolved against the
/// local exercise DB by CodeNum) and CustomExercises into one list for rendering. CodeNum is 0
/// for custom exercises, matching how the server itself distinguishes the two on share.
/// (The wire DTOs - SocialWorkoutDto, ShareSocialWorkoutRequest, SocialWorkoutBaseExerciseDto,
/// SocialWorkoutExerciseDto, PagedResult&lt;T&gt; - already exist in TrainingDay.Common.Models,
/// shared with the server, and are used directly rather than duplicated here.)
/// </summary>
public class SocialWorkoutExerciseDisplay
{
    public string ExerciseName { get; set; }

    public List<string> MusclesString { get; set; } = new();

    public string WeightAndRepsString { get; set; }

    public List<string> TagsValue { get; set; } = new();

    public int CodeNum { get; set; }
}
