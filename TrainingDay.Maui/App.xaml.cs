using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using CommunityToolkit.Mvvm.Messaging;
using SentinelAnalytics.Maui;
using System.Globalization;
using System.Net;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Models.Messages;
using TrainingDay.Maui.Models.Notifications;
using TrainingDay.Maui.Models.Serialize;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui
{
    public partial class App : Application
    {
        private IPushNotification notificationService;
        private IDataService dataService;
        
        private static Repository database;
        private static object lockBase = new object();

        public static Repository Database
        {
            get
            {
                lock (lockBase)
                {
                    if (database == null)
                    {
                        database = new Repository(ConstantKeys.DatabaseName);
                    }

                    return database;
                }
            }
        }
        public bool IsTrainingNotFinished => Settings.IsTrainingNotFinished;
        public string MeasureOfWeight { get; set; }
        public ToolTipManager ToolTipManager { get; set; }

        public App()
        {
            InitializeComponent();
            LocalizationResourceManager.Instance.SetCulture(Settings.GetLanguage());
            ToolTipManager = new ToolTipManager();

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Exception ex = (Exception)args.ExceptionObject;
                LoggingService.TrackError(ex);
            };
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
        
        protected override void OnStart()
        {
            base.OnStart();

            SentinelTracker.Initialize(ConstantKeys.SentinelTrackerProjectId);

            Settings.LastDatabaseSyncDateTime = Settings.LastDatabaseSyncDateTime.IsNotNullOrEmpty() ? Settings.LastDatabaseSyncDateTime : DateTime.Now.ToString(Settings.GetLanguage());

            notificationService = Handler.MauiContext.Services.GetRequiredService<IPushNotification>();
            dataService = Handler.MauiContext.Services.GetRequiredService<IDataService>();
            MeasureOfWeight = GetMeasureOfWeight();

            Dispatcher.Dispatch(async () =>
            {
                await LoggingService.TrackEvent("Application start");
                await dataService.SendFirebaseTokenAsync(Settings.Token, CultureInfo.CurrentCulture.Name, TimeZoneInfo.Local.BaseUtcOffset.ToString());
                await dataService.PostActionAsync(Settings.Token, Common.Communication.MobileActions.Enter);
                await DownloadImagesAsync();
            });
        }

        private async Task DownloadImagesAsync()
        {
            try
            {
                string accessKey = ConstantKeys.AwsS3.accessKey;
                string secretKey = ConstantKeys.AwsS3.secretKey;

                var s3Client = new AmazonS3Client(
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
                        var response = await s3Client.GetObjectAsync(ConstantKeys.AwsS3.BucketName, b.Key);
                        if (response is null)
                            return;

                        var url = b.Key.Replace(".jpg", string.Empty).Replace(".png", string.Empty);

                        ImageEntity image = App.Database.GetImage(url) ?? new ImageEntity();

                        if (image.Data?.Length != response.Headers.ContentLength)
                        {
                            var path = await GetFile(response, b.Key);
                            var bytes = File.ReadAllBytes(path);
                            image.Data = bytes;
                            image.Url = url;

                            App.Database.SaveImage(image);
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.TrackError(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.TrackError(ex);
            }
        }

        public static async Task<string> GetFile(GetObjectResponse response, string key)
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var path = Path.Combine(documentsPath, key);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                await response.WriteResponseStreamToFileAsync(path, false, CancellationToken.None);
            }

            return path;
        }

        internal void SetIncomingFile(string data)
        {
            try
            {
                var vm = DataManageViewModel.LoadFromData(data);
                if (vm != null)
                {
                    IncomingTraining(vm);
                    notificationService.Show(new PushMessage()
                    {
                        Id = PushMessagesExtensions.WorkoutAddedId,
                        Title = AppResources.WorkoutAddedString,
                        Message = vm.Title,
                        IsDisableSwipe = true,
                        IsSilent = false,
                        IsUpdateCurrent = false,
                        Data = null
                    });
                    WeakReferenceMessenger.Default.Send<IncomingTrainingAddedMessage>();
                    return;
                }
            }
            catch (Exception ex)
            {
                LoggingService.TrackError(ex);
            }

            notificationService.Show(new PushMessage()
            {
                Id = PushMessagesExtensions.WorkoutAddedId,
                Title = AppResources.Denied,
                Message = AppResources.WorkoutAddedErrorString,
                IsDisableSwipe = true,
                IsSilent = false,
                IsUpdateCurrent = false,
                Data = null
            });
        }

        private void IncomingTraining(TrainingSerialize vm)
        {
            var exercises = Database.GetExerciseItems().ToList();
            var superSets = new List<Models.Database.SuperSetEntity>();

            var id = Database.SaveTrainingItem(new Models.Database.TrainingEntity() { Title = vm.Title });

            foreach (var item in vm.Items)
            {
                var exercise = exercises.FirstOrDefault(a => a.CodeNum == item.CodeNum);
                int exerciseId;
                if (exercise != null && item.CodeNum != 0)
                {
                    exerciseId = exercise.Id;
                }
                else
                {
                    var newItem = new Models.Database.ExerciseEntity()
                    {
                        Description = item.Description,
                        Name = item.Name,
                        MusclesString = item.Muscles,
                        TagsValue = item.TagsValue,
                        CodeNum = -1,
                    };
                    exerciseId = Database.SaveExerciseItem(newItem);

                    newItem.Id = exerciseId;
                    exercises.Add(newItem);
                }

                int superSetId;
                if (item.SuperSetId == 0)
                {
                    superSetId = 0;
                }
                else
                {
                    var superSet = superSets.FirstOrDefault(a => a.Count == item.SuperSetId);

                    if (superSet != null)
                    {
                        superSetId = superSet.Id;
                    }
                    else
                    {
                        var newItem = new Models.Database.SuperSetEntity()
                        {
                            TrainingId = id,
                        };
                        superSetId = Database.SaveSuperSetItem(newItem);

                        newItem.Count = item.SuperSetId;
                        newItem.Id = superSetId;
                        superSets.Add(newItem);
                    }
                }

                Database.SaveTrainingExerciseItem(new Models.Database.TrainingExerciseEntity()
                {
                    OrderNumber = item.OrderNumber,
                    TrainingId = id,
                    SuperSetId = superSetId,
                    ExerciseId = exerciseId,
                    WeightAndRepsString = item.WeightAndRepsString,
                });
            }
        }

        private static string GetMeasureOfWeight()
        {
            List<Tuple<MeasureWeightTypes, string>> items =
            [
                new Tuple<MeasureWeightTypes, string>(MeasureWeightTypes.Kilograms, AppResources.KilogramsString),
                new Tuple<MeasureWeightTypes, string>(MeasureWeightTypes.Lbs, AppResources.LbsString),
            ];

            return items.FirstOrDefault(item => (int)item.Item1 == Settings.WeightMeasureType)!.Item2;
        }
    }
}
