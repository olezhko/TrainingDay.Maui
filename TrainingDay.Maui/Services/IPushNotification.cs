using TrainingDay.Maui.Models.Notifications;

namespace TrainingDay.Maui.Services;

public interface IPushNotification
{
    void Show(PushMessage message);
    void Cancel(int id);
}