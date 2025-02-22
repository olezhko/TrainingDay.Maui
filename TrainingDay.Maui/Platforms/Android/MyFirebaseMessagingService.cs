using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Util;
using AndroidX.Core.App;
using Firebase.Messaging;
using TrainingDay.Common;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Services;
using Application = Android.App.Application;

namespace TrainingDay.Maui.Platforms.Android
{
    [Service(Exported = true)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseMessagingService : FirebaseMessagingService
    {
        public static string webContentList = "";
        const string TAG = "MyFirebaseMsgService";
        public override void OnNewToken(string p0)
        {
            base.OnNewToken(p0);
            Log.Debug(TAG, "Refreshed token: " + p0);
            App.SendRegistrationToServer(p0);
            TrainingDay.Maui.Services.Settings.Token = p0;
        }

        public override void OnMessageReceived(RemoteMessage message)
        {
            try
            {
                var notify = message.GetNotification();
                string type = string.Empty;
                string title = string.Empty;
                string text = string.Empty;
                try
                {
                    if (notify != null)
                    {
                        title = notify.Title;
                        text = notify.Body;
                    }
                    message.Data.TryGetValue("type", out type);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                if (!string.IsNullOrEmpty(type))
                {
                    switch (type)
                    {
                        case PushNotificationData.WeightType:
                            if (PushMessagesExtensions.IsShowWeightNotify())
                            {
                                ShowPushNotification(new PushMessage()
                                {
                                    Title = PushMessagesExtensions.WeightMessageTitle,
                                    Message = PushMessagesExtensions.WeightMessage,
                                    Id = PushMessagesExtensions.WeightNotificationId,
                                    Data = message.Data,
                                });
                                PushMessagesExtensions.WeightNotificationState = true;
                            }
                            break;
                        case PushNotificationData.WorkoutType:
                            if (true)
                            {
                                ShowPushNotification(new PushMessage()
                                {
                                    Title = PushMessagesExtensions.NewWorkoutMessageTitle,
                                    Message = PushMessagesExtensions.ReturnToTrainingMessage,
                                    Id = PushMessagesExtensions.TrainingNotificationId,
                                    Data = message.Data,
                                });
                                PushMessagesExtensions.TrainingNotificationState = true;
                            }
                            break;
                        case PushNotificationData.BlogType:
                            ShowPushNotification(new PushMessage()
                            {
                                Title = title,
                                Message = text,
                                Id = PushMessagesExtensions.NewBlogId,
                                Data = message.Data,
                            });
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                LoggingService.TrackError(e);
            }
        }

        public static void ShowPushNotification(PushMessage message)
        {
            var valuesForActivity = new Bundle();
            if (message.Data != null)
                foreach (var key in message.Data.Keys)
                {
                    valuesForActivity.PutString(key, message.Data[key]);
                    if (key == "pushData")
                    {
                        webContentList = message.Data[key];
                    }
                }

            Intent intent = new Intent(Application.Context, typeof(MainActivity));
            intent.PutExtra("title", message.Title);
            intent.PutExtra("message", message.Message);

            intent.PutExtras(valuesForActivity);

            var pendingIntentFlag = message.IsUpdateCurrent ? PendingIntentFlags.UpdateCurrent : PendingIntentFlags.OneShot;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                pendingIntentFlag = PendingIntentFlags.Mutable;
            }

            PendingIntent pendingIntent = PendingIntent.GetActivity(Application.Context, 0, intent, pendingIntentFlag);

            NotificationCompat.Builder builder = new NotificationCompat.Builder(Application.Context, message.IsSilent ? MainActivity.SilentChannel : MainActivity.AppChannel)
                .SetContentIntent(pendingIntent)
                .SetContentTitle(message.Title)
                .SetContentText(message.Message)
                .SetLargeIcon(BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.main))
                .SetSmallIcon(Resource.Drawable.main)
                .SetPriority((int)NotificationPriority.Low)
                .SetVisibility((int)NotificationVisibility.Public)
                .SetOngoing(message.IsDisableSwipe); // disable swipe

            Notification notification = builder.Build();
            var notificationManager = NotificationManagerCompat.From(Application.Context);
            notificationManager.Notify(message.Id, notification);
        }
    }
}
