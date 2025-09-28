namespace TrainingDay.Maui.Models.Notifications;

public class PushMessage
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string Message { get; set; }

    public IDictionary<string, string>? Data { get; set; } = null;

    public bool IsUpdateCurrent { get; set; }

    public bool IsSilent { get; set; }

    public bool IsDisableSwipe { get; set; }
}