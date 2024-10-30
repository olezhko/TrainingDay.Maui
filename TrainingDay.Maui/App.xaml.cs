using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System.Globalization;
using System.Net;
using TrainingDay.Common;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui
{
    public partial class App : Application
    {
        private const string DatabaseName = "exercise.db";
        private static Repository database;
        private static object lockBase = new object();
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
#if !DEBUG || !IOS
            AppCenter.Start(DeviceInfo.Platform == DevicePlatform.iOS ? "59807e71-013b-4d42-9306-4a6044d9dc5f" : "96acc322-4770-4aa3-876b-16ce5a802a38", typeof(Analytics), typeof(Crashes));
#endif

            Settings.LastDatabaseSyncDateTime = Settings.LastDatabaseSyncDateTime.IsNotNullOrEmpty() ? Settings.LastDatabaseSyncDateTime : DateTime.Now.ToString(Settings.GetLanguage());

            Analytics.TrackEvent("Application Started");

            DownloadImages();
        }

        private async void DownloadImages()
        {
            try
            {
                string accessKey = ConstantKeys.AwsS3.accessKey;
                string secretKey = ConstantKeys.AwsS3.secretKey;

                AmazonS3Client s3Client = new AmazonS3Client(
                        accessKey,
                        secretKey,
                        RegionEndpoint.GetBySystemName("us-east-1")
                        );

                var request = new ListObjectsV2Request()
                {
                    BucketName = ConstantKeys.AwsS3.BucketName,
                };

                var result = await s3Client.ListObjectsV2Async(request);
                foreach (var b in result.S3Objects)
                {
                    try
                    {
                        var url = b.Key.Replace(".jpg", string.Empty);
                        ImageData image = App.Database.GetImage(url);
                        if (image == null)
                        {
                            var path = await GetFile(s3Client, b.Key);
                            var bytes = File.ReadAllBytes(path);
                            var item = new ImageData()
                            {
                                Data = bytes.ToArray(),
                                Url = url,
                            };

                            App.Database.SaveImage(item);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (System.Exception ex)
            {
            }
        }

        public async Task<string> GetFile(AmazonS3Client amazonS3Client, string key)
        {
            var response = await amazonS3Client.GetObjectAsync(ConstantKeys.AwsS3.BucketName, key);

            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var path = Path.Combine(documentsPath, key);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                await response.WriteResponseStreamToFileAsync(path, false, CancellationToken.None);
            }

            return path;
        }

        public static async void SendRegistrationToServer(string token)
        {
            if (DeviceInfo.DeviceType != DeviceType.Physical)
            {
                return;
            }

            try
            {
                SiteService.Token = token;
                if (string.IsNullOrEmpty(token))
                {
                    return;
                }

                Settings.IsTokenSavedOnServer = false;

                var language = Settings.GetLanguage();
                var zone = TimeZoneInfo.Local.BaseUtcOffset;
                var res = await SiteService.SendTokenToServer(token, language.Name, zone, 7);
                Settings.IsTokenSavedOnServer = res;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
