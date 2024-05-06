using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using AndroidX.Core.App;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Services;
using Application = Android.App.Application;

namespace TrainingDay.Maui.Platforms.Android;

internal class PushNotificationService : IPushNotification
{
    public static string webContentList = "";
    public void Cancel(int id)
    {
        var notificationManager = NotificationManagerCompat.From(Application.Context);
        notificationManager.Cancel(id);
    }

    public void Show(PushMessage message)
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