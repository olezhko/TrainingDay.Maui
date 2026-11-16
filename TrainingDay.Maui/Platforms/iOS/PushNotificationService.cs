using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models.Notifications;
using TrainingDay.Maui.Services;
using UserNotifications;

namespace TrainingDay.Maui.Platforms.iOS
{
	public class PushNotificationService : IPushNotification
    {
        private const UNAuthorizationOptions RequestedOptions = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound;

        public PushNotificationService()
		{
            // Create a UNUserNotificationCenterDelegate to handle incoming messages.
            UNUserNotificationCenter.Current.Delegate = new iOSNotificationReceiver();

            // Request permission to use local notifications.
            UNUserNotificationCenter.Current.RequestAuthorization(RequestedOptions, (approved, err) =>
            {
                if (err != null)
                {
                    LoggingService.TrackError(new Exception($"Notification authorization failed: {err}"));
                }
            });
        }

        public void Cancel(int id)
        {
            var ids = new[] { id.ToString() };
            UNUserNotificationCenter.Current.RemovePendingNotificationRequests(ids);
            UNUserNotificationCenter.Current.RemoveDeliveredNotifications(ids);
        }

        public void Show(PushMessage message)
        {
            // Check the actual, current authorization state instead of a cached flag:
            // the user can change it in Settings at any time, and the initial request is async.
            UNUserNotificationCenter.Current.GetNotificationSettings(settings =>
            {
                var status = settings.AuthorizationStatus;
                if (status != UNAuthorizationStatus.Authorized
                    && status != UNAuthorizationStatus.Provisional
                    && status != UNAuthorizationStatus.Ephemeral)
                {
                    return;
                }

                if (settings.AlertSetting != UNNotificationSetting.Enabled)
                {
                    return;
                }

                var content = new UNMutableNotificationContent()
                {
                    Title = message.Title ?? string.Empty,
                    Body = message.Message ?? string.Empty,
                };

                if (!message.IsSilent && settings.SoundSetting == UNNotificationSetting.Enabled)
                {
                    content.Sound = UNNotificationSound.Default;
                }

                // Interval is in seconds and must be greater than 0.
                var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(0.25, false);
                var request = UNNotificationRequest.FromIdentifier(message.Id.ToString(), content, trigger);

                UNUserNotificationCenter.Current.AddNotificationRequest(request, err =>
                {
                    if (err != null)
                    {
                        LoggingService.TrackError(new Exception($"Failed to schedule notification {message.Id}: {err}"));
                    }
                });
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
                completionHandler(UNNotificationPresentationOptions.Banner | UNNotificationPresentationOptions.List);
            }
        }
    }
}
