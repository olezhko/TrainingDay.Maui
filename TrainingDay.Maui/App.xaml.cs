using CommunityToolkit.Maui;
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
        public static Options Options;

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
                Thread.Sleep(300);
            };

            RequestedThemeChanged += (sender, args) => {
                Settings.IsLightTheme = App.Current.RequestedTheme == AppTheme.Light;
                App.Options.SetPopupDefaults(new DefaultPopupSettings
                {
                    CanBeDismissedByTappingOutsideOfPopup = true,
                    Padding = 4,
                    Margin = 10,
                    // because options not handle theme change
                    BackgroundColor = Settings.IsLightTheme ? Color.FromArgb("#f0f0f0") : Color.FromArgb("#1b1b1b")
                });
            };
        }

        protected override Window CreateWindow(IActivationState? activationState) => new Window(new AppShell());

        protected override void OnStart()
        {
            base.OnStart();
            Settings.IsLightTheme = App.Current.RequestedTheme == AppTheme.Light;

#if     DEBUG
            SentinelTracker.Initialize(ConstantKeys.SentinelTrackerProjectId, false);
#elif   RELEASE
            SentinelTracker.Initialize(ConstantKeys.SentinelTrackerProjectId, true);
#endif

            Settings.LastDatabaseSyncDateTime = Settings.LastDatabaseSyncDateTime.IsNotNullOrEmpty() ? Settings.LastDatabaseSyncDateTime : DateTime.Now.ToString(Settings.GetLanguage());

            notificationService = Handler.MauiContext.Services.GetRequiredService<IPushNotification>();
            dataService = Handler.MauiContext.Services.GetRequiredService<IDataService>();
            MeasureOfWeight = GetMeasureOfWeight();

            Dispatcher.Dispatch(async () =>
            {
                await dataService.SendFirebaseTokenAsync(Settings.Token, CultureInfo.CurrentCulture.Name, TimeZoneInfo.Local.BaseUtcOffset.ToString());
                await dataService.PostActionAsync(Settings.Token, Common.Communication.MobileActions.Enter);
                await DownloadImagesAsync();
            });
        }

        private async Task DownloadImagesAsync()
        {
            int maxCode = 143;
            HttpClient httpClient = new HttpClient();
            for (int i = 1; i < maxCode; i++)
            {
                try
                {
                    var urlToDownload = @$"https://api.trainingday.space/exercise_images/{i}.jpg";
                    var response = await httpClient.GetByteArrayAsync(urlToDownload);
                    if (response is null)
                        return;

                    var url = $"{i}";

                    ImageEntity image = App.Database.GetImage(url) ?? new ImageEntity();

                    if (image.Data?.Length != response.Length)
                    {
                        image.Data = response;
                        image.Url = url;

                        App.Database.SaveImage(image);
                    }
                }
                catch
                {
                }
            }
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

        private static void IncomingTraining(TrainingSerialize vm)
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
