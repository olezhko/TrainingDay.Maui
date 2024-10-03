using System;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.Platforms.iOS
{
	public class PushNotificationService : IPushNotification
    {
		public PushNotificationService()
		{
		}

        public void Cancel(int id)
        {
        }

        public void Show(PushMessage message)
        {
        }
    }
}

