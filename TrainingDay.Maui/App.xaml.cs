using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System.Globalization;
using TrainingDay.Common;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui
{
    public partial class App : Application
    {
        private const string DatabaseName = "exercise.db";
        private static Repository database;
        private static object lockBase = new object();
        private static ImageQueueCacheDownloader imageCache;
        #region To Remove
        public static CultureInfo CurrentCultureForEntryDot => DeviceInfo.Platform == DevicePlatform.Android ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
        #endregion
        public static Repository Database
        {
            get
            {
                lock (lockBase)
                {
                    if (database == null)
                    {
                        database = new Repository(DatabaseName);
                    }

                    return database;
                }
            }
        }
        public bool IsTrainingNotFinished => Settings.IsTrainingNotFinished;
        public static double FullWidth => DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
        public static ImageQueueCacheDownloader ImageDownloader
        {
            get
            {
                if (imageCache == null)
                {
                    imageCache = new ImageQueueCacheDownloader();
                }

                return imageCache;
            }
        }

        public ToolTipManager ToolTipManager { get; set; }

        public App()
        {
            InitializeComponent();
            LocalizationResourceManager.Instance.SetCulture(Settings.GetLanguage());
            MainPage = new AppShell();
            ToolTipManager = new ToolTipManager();

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Exception ex = (Exception)args.ExceptionObject;
                Crashes.TrackError(ex);
            };
        }

        protected override void OnStart()
        {
            base.OnStart();
            //AppCenter.Start(DeviceInfo.Platform == DevicePlatform.iOS ? "59807e71-013b-4d42-9306-4a6044d9dc5f" : "96acc322-4770-4aa3-876b-16ce5a802a38", typeof(Analytics), typeof(Crashes));

            Settings.LastDatabaseSyncDateTime = Settings.LastDatabaseSyncDateTime.IsNotNullOrEmpty() ? Settings.LastDatabaseSyncDateTime : DateTime.Now.ToString(Settings.GetLanguage());

            Analytics.TrackEvent("Application Started");
        }

        public static void SyncAlarms()
        {
            Task.Run(async () =>
            {
                var result = await SiteService.DeleteRepoItems(SyncItemType.Alarm);
                if (result)
                {
                    var alarms = Database.GetAlarmItems();
                    foreach (var alarm in alarms)
                    {
                        if (!alarm.IsActive)
                        {
                            continue;
                        }

                        var serverId = await SiteService.UploadRepoItem(alarm, SyncItemType.Alarm);
                        alarm.ServerId = serverId;
                        if (serverId != 0)
                        {
                            Database.SaveAlarmItem(alarm);
                        }
                    }
                }
            });
        }
    }
}
