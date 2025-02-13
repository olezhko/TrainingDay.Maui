using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Services;
using UserNotifications;

namespace TrainingDay.Maui.Platforms.iOS
{
	public class PushNotificationService : IPushNotification
    {
        bool hasNotificationsPermission;

        public PushNotificationService()
		{
            // Create a UNUserNotificationCenterDelegate to handle incoming messages.
            UNUserNotificationCenter.Current.Delegate = new iOSNotificationReceiver();

            // Request permission to use local notifications.
            UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Alert, (approved, err) =>
            {
                hasNotificationsPermission = approved;
            });
        }

        public void Cancel(int id)
        {
            UNUserNotificationCenter.Current.RemoveDeliveredNotifications(new[] { id.ToString() });
        }

        public void Show(PushMessage message)
        {
            if (!hasNotificationsPermission)
                return;

            var content = new UNMutableNotificationContent()
            {
                Title = message.Title,
                Subtitle = "",
                Body = message.Message,
                Badge = 1
            };

            UNNotificationTrigger trigger;
            // Create a time-based trigger, interval is in seconds and must be greater than 0.
            trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(0.25, false);

            var request = UNNotificationRequest.FromIdentifier(message.Id.ToString(), content, trigger);
            UNUserNotificationCenter.Current.AddNotificationRequest(request, (err) =>
            {
                if (err != null)
                    throw new Exception($"Failed to schedule notification: {err}");
            });
        }
    }

    public class iOSNotificationReceiver : UNUserNotificationCenterDelegate
    {
        public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            if (notification.Request.Identifier == PushMessagesExtensions.TrainingImplementTimeId.ToString())
            {
                completionHandler(UNNotificationPresentationOptions.None);
            }
            else
            {
                completionHandler(UNNotificationPresentationOptions.Banner);
            }
        }
    }
}

