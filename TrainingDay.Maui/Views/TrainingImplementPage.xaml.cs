using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Common.Extensions;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Controls;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models.Messages;
using TrainingDay.Maui.Models.Notifications;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using LastTraining = TrainingDay.Maui.Models.Database.LastTrainingEntity;
using LastTrainingExercise = TrainingDay.Maui.Models.Database.LastTrainingExerciseEntity;
using TrainingExerciseComm = TrainingDay.Maui.Models.Database.TrainingExerciseEntity;

namespace TrainingDay.Maui.Views;

[QueryProperty(nameof(TrainingItem), "TrainingItem")]
[QueryProperty(nameof(StartTime), "StartTime")]
public partial class TrainingImplementPage : ContentPage
{
    private readonly DateTime _startTrainingDateTime;
    private bool enabledTimer;
    private readonly IDispatcherTimer _timer;
    private IPushNotification notificator;
    private IDataService dataService;
    private TrainingViewModel trainingItem;
    private SuperSetViewModel currentSuperSet;

    public TrainingImplementPage()
    {
        InitializeComponent();
        Items = new ObservableCollection<SuperSetViewModel>();
        BindingContext = this;
        _startTrainingDateTime = DateTime.Now;
        enabledTimer = true;
        StepProgressBarControl.PropertyChanged += StepProgressBarControl_PropertyChanged;
        StepProgressBarControl.CurrentItemChanged += StepProgressBarControl_CurrentItemChanged;
        Settings.IsTrainingNotFinished = true;

        _timer = Shell.Current.Dispatcher.CreateTimer();
        _timer.Tick += _timer_Tick;
        _timer.Interval = TimeSpan.FromSeconds(1);

        RestPicker.TextColor = App.Current.RequestedTheme == AppTheme.Light ? Colors.Black : Colors.White;
    }

    private async void StepProgressBarControl_CurrentItemChanged(object? sender, StepProgressBarCurrentItemChangedEventArgs e)
    {
        var currentSuperSet = e.CurrentItem as SuperSetViewModel;
        await LoadVideoItemsAsync(currentSuperSet);
    }

    private void StepProgressBarControl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StepProgressBar.StepSelected))
        {
            SkipButton.IsVisible = FinishButton.IsVisible = Items[StepProgressBarControl.StepSelected].First().IsNotFinished;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        HandlerChanged += OnHandlerChanged;

        if (StepProgressBarControl.ItemsSource == null)
        {
            Items = TrainingItem.ExercisesBySuperSet;
            StepProgressBarControl.ItemsSource = Items;
            OnPropertyChanged(nameof(Items));

            DeviceDisplay.KeepScreenOn = Settings.IsDisplayOnImplement;

            int index = 0;
            foreach (var item in Items)
            {
                if (index == 0)
                {
                    currentSuperSet = item;
                }

                if (!item.SuperSetItems.First().IsNotFinished)
                {
                    StepProgressBarControl.DeselectElement();
                }

                if (item.SuperSetItems.First().IsSkipped)
                {
                    StepProgressBarControl.SkipElement();
                }

                index++;
                StepProgressBarControl.NextElement(index);
            }

            StepProgressBarControl.NextElement(FirstIndexIsNotFinished());
        }

        await ToolTipCancelImplementingTraining.Show();

        _timer.Start();
    }

    private async Task LoadVideoItemsAsync(SuperSetViewModel item)
    {
        IsVideoLoading = true;
        foreach (var exercise in item.SuperSetItems)
        {
            if (exercise.VideoItems.Any())
            {
                continue;
            }

            if (dataService == null)
            {
                return;
            }

            try
            {
                var videoUrls = await dataService.GetVideosAsync(exercise.Name);
                foreach (var video in videoUrls)
                {
                    video.VideoUrl = await GetVideoURLAsync("http://www.youtube.com/watch?v=" + video.VideoUrl);
                }

                exercise!.VideoItems = videoUrls.Select(item => new ExerciseVideo()
                {
                    VideoUrl = item.VideoUrl
                }).ToObservableCollection();
            }
            catch (Exception ex)
            {

            }
        }

        IsVideoLoading = false;
    }

    private static async Task<string> GetVideoURLAsync(string url)
    {
        return await Task.Run(async () =>
        {
            var youtube = new YoutubeClient();

            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(url);

            IEnumerable<MuxedStreamInfo> streamInfo = streamManifest.GetMuxedStreams();

            var videoPlayerStream = streamInfo.First(video => video.VideoQuality.Label is "240p" or "360p" or "480p");

            return videoPlayerStream.Url;
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StepProgressBarControl.PropertyChanged -= StepProgressBarControl_PropertyChanged;
        HandlerChanged -= OnHandlerChanged;
    }

    private async void OnHandlerChanged(object sender, EventArgs e)
    {
        if (Handler != null)
        {
            notificator = Handler.MauiContext.Services.GetRequiredService<IPushNotification>();
            dataService = Handler.MauiContext.Services.GetRequiredService<IDataService>();

            await LoadVideoItemsAsync(currentSuperSet);
        }
    }

    protected override bool OnBackButtonPressed() => true; // cancel back button

    #region Timer
    private void _timer_Tick(object? sender, EventArgs e)
    {
        CurrentTime = (DateTime.Now - _startTrainingDateTime + StartTime).ToString(@"hh\:mm\:ss");
        OnPropertyChanged(nameof(CurrentTime));

        if (isStartRest && RestPicker.Value > TimeSpan.FromSeconds(0))
        {
            RestPicker.Value = RestPicker.Value - TimeSpan.FromSeconds(1);
            if (RestPicker.Value < TimeSpan.FromSeconds(10))
            {
                RestPicker.TextColor = Colors.Red;
                PlaySound();
            }
            else
            {
                RestPicker.TextColor = App.Current.RequestedTheme == AppTheme.Light ? Colors.Black : Colors.White;
            }
        }
        else {
            RestOrDoSwitch.IsToggled = false;
            isStartRest = false;
        }

        UpdateExerciseTime();
        // save to restore if not finish
        SaveNotFinishedTraining(TrainingItem.Title, TrainingItem.Id);
        UpdateNotifyTimer();

        if (!enabledTimer)
        {
            _timer.Stop();
        }
    }

    private void PlaySound()
    {
#if ANDROID
            var instance = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            Android.Net.Uri uri = Android.Media.RingtoneManager.GetDefaultUri(Android.Media.RingtoneType.Notification);
            Android.Media.Ringtone rt = Android.Media.RingtoneManager.GetRingtone(instance.ApplicationContext, uri);
            rt.Play();
#endif
    }

    private void SaveNotFinishedTraining(string title, int id)
    {
        Settings.IsTrainingNotFinishedTime = CurrentTime;
        var filename = Path.Combine(FileSystem.CacheDirectory, ConstantKeys.NotFinishedTrainingName);

        TrainingViewModel training = new TrainingViewModel();
        training.Id = id;
        training.Title = title;

        foreach (var item in Items)
        {
            foreach (var trainingExerciseViewModel in item.SuperSetItems)
            {
                training.AddExercise(trainingExerciseViewModel);
            }
        }

        training.SaveToFile(filename);
    }

    private void UpdateNotifyTimer()
    {
        if (enabledTimer)
        {
            if (Items.Count > 0)
            {
                var name = string.Join(" - ", Items[StepProgressBarControl.StepSelected].Select(a => a.Name));
                notificator.Show(new PushMessage()
                {
                    Id = PushMessagesExtensions.TrainingImplementTimeId,
                    Title = name,
                    Message = CurrentTime,
                    IsDisableSwipe = false,
                    IsSilent = true,
                    IsUpdateCurrent = true,
                    Data = null
                });
            }
        }
    }

    private void UpdateExerciseTime()
    {
        try
        {
            var exercises = Items[StepProgressBarControl.StepSelected];
            foreach (var item in exercises)
            {
                if (item.Tags.Contains(ExerciseTags.ExerciseByTime) && item.IsTimeCalculating && item.IsNotFinished)
                {
                    item.Time = DateTime.Now - item.StartCalculateDateTime;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    #endregion

    #region Finish
    private async void FinishButtonClicked(object sender, TappedEventArgs e)
    {
        try
        {
            Items[StepProgressBarControl.StepSelected].ForEach(item => item.IsNotFinished = false);
            Items[StepProgressBarControl.StepSelected].ForEach(item => item.IsSkipped = false);

            if (Items.All(a => a.All(item => !item.IsNotFinished || item.IsSkipped)) && enabledTimer)
            {
                await FinishAndSave();
                LoggingService.TrackEvent($"{GetType().Name}: Training Implementing Finished");
            }
            else
            {
                StepProgressBarControl.DeselectElement();
                StepProgressBarControl.NextElement(FirstIndexIsNotFinished());
            }
        }
        catch (Exception ex)
        {
            LoggingService.TrackError(ex);
            await Shell.Current.GoToAsync("..");
        }
    }

    private int FirstIndexIsNotFinished()
    {
        int index = 0;
        foreach (var trainingExerciseViewModel in Items)
        {
            if (trainingExerciseViewModel.First().IsNotFinished && !trainingExerciseViewModel.First().IsSkipped)
            {
                return index;
            }

            index++;
        }

        return index;
    }

    private async Task FinishAndSave()
    {
        enabledTimer = false;
        notificator.Cancel(PushMessagesExtensions.TrainingImplementTimeId);
        Settings.IsTrainingNotFinished = false;
        SaveLastTraining();
        SaveChangedExercises();

        if (Settings.IsShowAdvicesOnImplementing)
        {
            await MessageManager.DisplayAlert(AppResources.AdviceString, AppResources.AdviceAfterTrainingMessage, AppResources.OkString);
        }

        notificator.Cancel(PushMessagesExtensions.TrainingNotificationId);
        Shell.SetNavBarIsVisible(this, true);
        await Shell.Current.GoToAsync("//workouts");

        await dataService.PostActionAsync(Settings.Token, Common.Communication.MobileActions.Workout);
    }

    private void SaveLastTraining()
    {
        App.Database.SaveLastTrainingItem(new LastTraining()
        {
            ElapsedTime = DateTime.Now - _startTrainingDateTime + StartTime,
            Time = _startTrainingDateTime,
            Title = TrainingItem.Title,
            TrainingId = TrainingItem.Id,
        });

        var id = App.Database.GetLastInsertId();
        foreach (var superSet in Items)
        {
            foreach (var item in superSet)
            {
                if (item.IsSkipped)
                {
                    continue;
                }

                App.Database.SaveLastTrainingExerciseItem(new LastTrainingExercise()
                {
                    LastTrainingId = id,
                    OrderNumber = item.OrderNumber,
                    ExerciseName = item.Name,
                    MusclesString = MusclesExtensions.ConvertFromListToString(item.Muscles.ToList()),
                    Description = item.GetExercise().Description,
                    SuperSetId = item.SuperSetId,
                    TagsValue = ExerciseExtensions.ConvertTagListToInt(item.Tags),
                    CodeNum = item.CodeNum,
                    WeightAndRepsString = ExerciseManager.ConvertJson(item.Tags, item),
                });
            }
        }
    }

    private void SaveChangedExercises()
    {
        int order = 0;
        foreach (var superSet in Items)
        {
            foreach (var trainingExerciseViewModel in superSet)
            {
                order++;
                App.Database.SaveTrainingExerciseItem(new TrainingExerciseComm()
                {
                    ExerciseId = trainingExerciseViewModel.ExerciseId,
                    TrainingId = TrainingItem.Id,
                    OrderNumber = order,
                    Id = trainingExerciseViewModel.TrainingExerciseId,
                    SuperSetId = trainingExerciseViewModel.SuperSetId,
                    WeightAndRepsString = ExerciseManager.ConvertJson(trainingExerciseViewModel.Tags, trainingExerciseViewModel),
                });
            }
        }
    }
    #endregion

    private void SubscribeMessages()
    {
        UnsubscribeMessages();
        WeakReferenceMessenger.Default.Register<ExercisesSelectFinishedMessage>(this, (r, args) =>
        {
            AddExercises(args.Selected.Select(item => TrainingExerciseViewModel.Create(item.GetExercise())));

            UnsubscribeMessages();
            LoggingService.TrackEvent($"TrainingImplementPage: AddExercises finished");
        });
    }

    private void UnsubscribeMessages()
    {
        WeakReferenceMessenger.Default.Unregister<ExercisesSelectFinishedMessage>(this);
    }

    private async void AddExercisesRequest()
    {
        SubscribeMessages();
        Dictionary<string, object> param = new Dictionary<string, object> { { "ExistedExercises", TrainingItem.Exercises } };

        await Shell.Current.GoToAsync(nameof(ExerciseListPage), param);
    }

    private void AddExercises(IEnumerable<TrainingExerciseViewModel> selectedItems)
    {
        selectedItems.ForEach(a => a.IsSelected = false);
        foreach (var exerciseItem in selectedItems)
        {
            var newSuperSet = new SuperSetViewModel() { TrainingId = TrainingItem.Id };
            newSuperSet.Add(exerciseItem);
            Items.Add(newSuperSet);
            exerciseItem.TrainingId = TrainingItem.Id;
            exerciseItem.OrderNumber = Items.Count - 1;
            TrainingItem.AddExercise(exerciseItem);
            var id = App.Database.SaveTrainingExerciseItem(exerciseItem.GetTrainingExerciseComm());
            exerciseItem.TrainingExerciseId = id;
        }

        OnPropertyChanged(nameof(Items));
    }

    private async void CancelTrainingClicked(object sender, TappedEventArgs e)
    {
        var result = await MessageManager.DisplayAlert(AppResources.CancelTrainingQuestion, TrainingItem.Title, AppResources.YesString, AppResources.NoString);
        if (result)
        {
            enabledTimer = false;
            notificator.Cancel(PushMessagesExtensions.TrainingImplementTimeId);
            Settings.IsTrainingNotFinished = false;

            Shell.SetNavBarIsVisible(this, true);
            await Shell.Current.GoToAsync("..");
        }
    }

    private async void SkipButtonClicked(object sender, TappedEventArgs e)
    {
        Items[StepProgressBarControl.StepSelected].ForEach(item => item.IsSkipped = !item.IsSkipped); // reverse skipped in exercise

        // if ex or superset not skipped
        if (Items[StepProgressBarControl.StepSelected].First().IsSkipped)
        {
            StepProgressBarControl.SkipElement(); // make it skipped in step
            StepProgressBarControl.NextElement(FirstIndexIsNotFinished());
            if (Items.All(a => a.All(item => !item.IsNotFinished || item.IsSkipped)) && enabledTimer)
            {
                await FinishAndSave();
            }
        }
        else
        {
            StepProgressBarControl.SkipElement(false);
        }
    }

    double restSeconds = 120;
    bool isStartRest = false;
    private void RestOrDoSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            RestLabel.TextColor = Colors.Orange;
            DoLabel.TextColor = Colors.Gray;
            RestPicker.IsTimerDisabled = true;
            restSeconds = RestPicker.Value.TotalSeconds;
        }
        else
        {
            RestLabel.TextColor = Colors.Gray;
            DoLabel.TextColor = Colors.Orange;
            RestPicker.IsTimerDisabled = false;
            RestPicker.Value = TimeSpan.FromSeconds(restSeconds);
        }

        isStartRest = e.Value;
        RestPicker.TextColor = App.Current.RequestedTheme == AppTheme.Light ? Colors.Black : Colors.White;
    }

    #region Properties
    public ICommand AddExercisesCommand => new Command(AddExercisesRequest);

    public string CurrentTime { get; set; }

    public TimeSpan StartTime { get; set; }

    public ObservableCollection<SuperSetViewModel> Items { get; set; }

    public TrainingViewModel TrainingItem
    {
        get => trainingItem;
        set
        {
            trainingItem = value;
        }
    }

    public bool IsVideoLoading { get => field; set => field = value; }

    #endregion
}