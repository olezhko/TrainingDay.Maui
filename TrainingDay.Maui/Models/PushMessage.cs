namespace TrainingDay.Maui.Models;

public class PushMessage
{
    public int Id { get; set; }

    public string Title { get; set; }

    public string Message { get; set; }

    public IDictionary<string, string> Data { get; set; }

    public bool IsUpdateCurrent { get; set; }

    public bool IsSilent { get; set; }

    public bool IsDisableSwipe { get; set; }
}