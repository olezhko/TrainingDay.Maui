namespace TrainingDay.Maui.Models.Notifications;

public class MobileToken
{
    public required string Token { get; set; }
    public required string Language { get; set; }
    public required string Zone { get; set; }
    public int Frequency { get; set; }
}