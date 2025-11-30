using TrainingDay.Maui.Resources.Strings;

namespace TrainingDay.Maui.Extensions;

public class PushMessagesExtensions
{
    public static bool IsShowNewWorkoutNotify()
    {
        var lastTrainings = App.Database.GetLastTrainingItems();
        if (lastTrainings.Any())
        {
            lastTrainings = lastTrainings.OrderBy(item => item.Time);
            if (DateTime.Now - lastTrainings.Last().Time > TimeSpan.FromDays(7))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsShowWeightNotify()
    {
        var weight = App.Database.GetWeightNotesItems();
        if (weight.Any())
        {
            var last = weight.OrderBy(note => note.Date).Last();
            if (DateTime.Now - last.Date > TimeSpan.FromDays(7))
            {
                return true;
            }
        }
        else
        {
            return true;
        }

        return false;
    }

    public static int TrainingNotificationId { get; set; } = 100;

    public static int WeightNotificationId { get; set; } = 101;

    public static int TrainingImplementTimeId { get; set; } = 102;

    public static int NewBlogId { get; set; } = 103;

    public static int SyncFinishedId { get; set; } = 104;

    public static int WorkoutAddedId { get; set; } = 105;

    public static bool WeightNotificationState { get; set; }

    public static bool TrainingNotificationState { get; set; }

    public static string NewWorkoutMessageTitle { get; set; } = AppResources.TrainingString;

    public static string ReturnToTrainingMessage { get; set; } = AppResources.ReturnToTrainingMessage;

    public static string WeightMessageTitle { get; set; } = AppResources.WeightString;

    public static string WeightMessage { get; set; } = AppResources.PleaseEnterYourNewWeight;
}