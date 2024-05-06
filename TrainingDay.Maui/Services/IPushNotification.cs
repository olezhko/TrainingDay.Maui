using TrainingDay.Maui.Models;

namespace TrainingDay.Maui.Services;

public interface IPushNotification
{
    void Show(PushMessage message);
    void Cancel(int id);
}