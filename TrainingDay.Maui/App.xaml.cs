﻿using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
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
        private IPushNotification notificator;
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
        public static double FullWidth => DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
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
        
        private void SetStatusBarStyle()
        {
            CommunityToolkit.Maui.Core.Platform.StatusBar.SetColor(RequestedTheme == AppTheme.Light ? Color.FromRgba("#FFFFFFFF") : Color.FromRgba("#FF000000"));
            CommunityToolkit.Maui.Core.Platform.StatusBar.SetStyle(RequestedTheme == AppTheme.Light ? StatusBarStyle.DarkContent : StatusBarStyle.LightContent);
        }

        protected override void OnStart()
        {
            base.OnStart();
            AppCenter.Start(DeviceInfo.Platform == DevicePlatform.iOS ? "59807e71-013b-4d42-9306-4a6044d9dc5f" : "96acc322-4770-4aa3-876b-16ce5a802a38", typeof(Analytics), typeof(Crashes));
            Settings.LastDatabaseSyncDateTime = Settings.LastDatabaseSyncDateTime.IsNotNullOrEmpty() ? Settings.LastDatabaseSyncDateTime : DateTime.Now.ToString(Settings.GetLanguage());

            LoggingService.TrackEvent("Application Started");

            notificator = Handler.MauiContext.Services.GetRequiredService<IPushNotification>();
            dataService = Handler.MauiContext.Services.GetRequiredService<IDataService>();
            MeasureOfWeight = GetMeasureOfWeight();

            Dispatcher.Dispatch(async () =>
            {
                await DownloadImages();
                await dataService.PostActionAsync(Settings.Token, Common.Communication.MobileActions.Enter);
            });
        }

        private async Task DownloadImages()
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
                        var url = b.Key.Replace(".jpg", string.Empty).Replace(".png", string.Empty);
                        ImageDto image = App.Database.GetImage(url);
                        if (image == null)
                        {
                            var path = await GetFile(s3Client, b.Key);
                            var bytes = File.ReadAllBytes(path);
                            var item = new ImageDto()
                            {
                                Data = bytes.ToArray(),
                                Url = url,
                            };

                            App.Database.SaveImage(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.TrackError(ex);
                    }
                }
            }
            catch (System.Exception ex)
            {
                LoggingService.TrackError(ex);
            }
        }

        public static async Task<string> GetFile(AmazonS3Client amazonS3Client, string key)
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

        internal void SetIncomingFile(string data)
        {
            try
            {
                var vm = DataManageViewModel.LoadFromData(data);
                if (vm != null)
                {
                    IncomingTraining(vm);
                    notificator.Show(new PushMessage()
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

            notificator.Show(new PushMessage()
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
            var superSets = new List<Models.Database.SuperSetDto>();

            var id = Database.SaveTrainingItem(new Models.Database.TrainingDto() { Title = vm.Title });

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
                    var newItem = new Models.Database.ExerciseDto()
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
                        var newItem = new Models.Database.SuperSetDto()
                        {
                            TrainingId = id,
                        };
                        superSetId = Database.SaveSuperSetItem(newItem);

                        newItem.Count = item.SuperSetId;
                        newItem.Id = superSetId;
                        superSets.Add(newItem);
                    }
                }

                Database.SaveTrainingExerciseItem(new Models.Database.TrainingExerciseDto()
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

            return items.FirstOrDefault(item => (int)item.Item1 == Settings.WeightMeasureType).Item2;
        }
    }
}
