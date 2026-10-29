using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SkiaSharp.Extended.UI.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Controls;
using TrainingDay.Maui.Controls.PopUp;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Mappers;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Models.Messages;
using TrainingDay.Maui.Models.Notifications;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Views;

[QueryProperty(nameof(TrainingItem), "TrainingItem")]
[QueryProperty(nameof(StartTime), "StartTime")]
public partial class TrainingImplementPage : ContentPage
{
    // the real save happens when the app goes to background; this is only a fallback in case the app crashes
    private const int SaveEveryTicks = 15;

    private readonly DateTime _startTrainingDateTime;
    private Window? watchedWindow;
    private int ticksSinceSave;
    private bool enabledTimer;
    private readonly IDispatcherTimer _timer;
    private IPushNotification notificator;
    private IDataService dataService;
    private ISocialWorkoutsService socialWorkoutsService;
    private TrainingViewModel trainingItem;
    private SuperSetViewModel<ImplementTrainingExerciseViewModel> currentSuperSet;

    public TrainingImplementPage()
    {
        InitializeComponent();
        Items = new ObservableCollection<SuperSetViewModel<ImplementTrainingExerciseViewModel>>();
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
        if (IsLoaded)
        {
            var currentSuperSet = e.CurrentItem as SuperSetViewModel<ImplementTrainingExerciseViewModel>;
            await LoadVideoItemsAsync(currentSuperSet);
        }
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

        var isFirstAppearing = StepProgressBarControl.ItemsSource == null;
        if (isFirstAppearing)
        {
            Items = TrainingItem.ExercisesBySuperSet
                .Select(MapBaseSuperSetToImplementMapper.Map)
                .ToObservableCollection<SuperSetViewModel<ImplementTrainingExerciseViewModel>>();

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

        WatchWindow();

        await ToolTipCancelImplementingTraining.Show();

        _timer.Start();

        if (isFirstAppearing)
        {
            await RequestNotificationPermissionAsync();
        }
    }

    /// <summary>
    /// The workout timer is shown as a notification; Android 13+ blocks it until the user allows notifications.
    /// </summary>
    private static async Task RequestNotificationPermissionAsync()
    {
#if ANDROID
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (status != PermissionStatus.Granted)
                await Permissions.RequestAsync<Permissions.PostNotifications>();
        }
        catch (Exception ex)
        {
            LoggingService.TrackError(ex);
        }
#else
        await Task.CompletedTask;
#endif
    }

    private void WatchWindow()
    {
        if (watchedWindow != null || Window == null)
        {
            return;
        }

        watchedWindow = Window;
        watchedWindow.Stopped += OnWindowStopped;
        watchedWindow.Destroying += OnWindowStopped; // Android back button can skip Stopped
    }

    private void StopWatchingWindow()
    {
        if (watchedWindow == null)
        {
            return;
        }

        watchedWindow.Stopped -= OnWindowStopped;
        watchedWindow.Destroying -= OnWindowStopped;
        watchedWindow = null;
    }

    private void OnWindowStopped(object? sender, EventArgs e)
    {
        if (enabledTimer)
        {
            SaveNotFinishedTraining(TrainingItem);
        }
    }

    private async Task LoadVideoItemsAsync(SuperSetViewModel<ImplementTrainingExerciseViewModel> item)
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

                exercise!.VideoItems = videoUrls.Select(item => new ExerciseVideo()
                {
                    VideoUrl = item.VideoUrl
                }).ToObservableCollection();
            }
            catch
            {

            }
        }

        IsVideoLoading = false;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StepProgressBarControl.PropertyChanged -= StepProgressBarControl_PropertyChanged;
        HandlerChanged -= OnHandlerChanged;
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);

        // the page left the stack (finish, cancel, error, iOS swipe back) - nothing is left to resume;
        // only an app killed mid-workout keeps the unfinished state
        if (args.NavigationType is NavigationType.Pop or NavigationType.PopToRoot or NavigationType.Remove)
        {
            enabledTimer = false;
            StopWatchingWindow();
            ClearNotFinishedTraining();
        }
    }

    private static void ClearNotFinishedTraining()
    {
        Settings.IsTrainingNotFinished = false;
        Settings.IsTrainingNotFinishedTime = string.Empty;
        try
        {
            File.Delete(Path.Combine(FileSystem.CacheDirectory, ConstantKeys.NotFinishedTrainingName));
        }
        catch (Exception e)
        {
            LoggingService.TrackError(e);
        }
    }

    private async void OnHandlerChanged(object sender, EventArgs e)
    {
        if (Handler != null)
        {
            notificator = Handler.MauiContext.Services.GetRequiredService<IPushNotification>();
            dataService = Handler.MauiContext.Services.GetRequiredService<IDataService>();
            socialWorkoutsService = Handler.MauiContext.Services.GetRequiredService<ISocialWorkoutsService>();
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
        // save to restore if not finish; the first tick saves too, so the file exists before the restore check can see IsTrainingNotFinished
        if (enabledTimer && ticksSinceSave++ % SaveEveryTicks == 0)
        {
            SaveNotFinishedTraining(TrainingItem);
        }

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

    private void SaveNotFinishedTraining(TrainingViewModel trainingViewModel)
    {
        Settings.IsTrainingNotFinishedTime = CurrentTime;
        var filename = Path.Combine(FileSystem.CacheDirectory, ConstantKeys.NotFinishedTrainingName);

        trainingViewModel.SaveToFile(filename);
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
        if (!enabledTimer)
        {
            return; // already finishing (confetti is playing)
        }

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
            StopWatchingWindow();
            ClearNotFinishedTraining();
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
        StopWatchingWindow();
        notificator.Cancel(PushMessagesExtensions.TrainingImplementTimeId);
        ClearNotFinishedTraining();

        // confetti starts right away; DB writes run off the UI thread meanwhile, sharing never blocks the finish
        var confettiTask = ShowConfettiAsync();
        var elapsedTime = DateTime.Now - _startTrainingDateTime + StartTime;
        await Task.Run(() => App.Database.RunInTransaction(() =>
        {
            SaveLastTrainingWithExercises(elapsedTime);
            SaveChangedExercises();
        }));
        _ = ShareToSocialFeedIfEnabled(elapsedTime);

        await confettiTask;

        if (Settings.IsShowAdvicesOnImplementing)
        {
            await Shell.Current.ShowPopupAsync(
                PopupBuilders.BuildAdvicePopup(AppResources.AdviceAfterTrainingMessage),
                PopupBuilders.DefaultPopupOptions);
        }

        notificator.Cancel(PushMessagesExtensions.TrainingNotificationId);
        Shell.SetNavBarIsVisible(this, true);
        await Shell.Current.GoToAsync("//workouts");

        _ = PostWorkoutActionAsync();
    }

    private async Task PostWorkoutActionAsync()
    {
        try
        {
            await dataService.PostActionAsync(Settings.Token, Common.Communication.MobileActions.Workout);
        }
        catch (Exception ex)
        {
            LoggingService.TrackError(ex);
        }
    }

    private async Task ShowConfettiAsync()
    {
        var confetti = new SKLottieView
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            Source = new SKFileLottieImageSource { File = "confetti.json" },
            RepeatCount = 0,
            RepeatMode = SKLottieRepeatMode.Restart,
            IsAnimationEnabled = true,
        };
        Grid.SetRowSpan(confetti, 4);
        Grid.SetColumnSpan(confetti, 3);
        MainGrid.Add(confetti);

        await Task.Delay(1500);

        MainGrid.Remove(confetti);
    }

    private void SaveLastTrainingWithExercises(TimeSpan elapsedTime)
    {
        var id = App.Database.SaveItem(new LastTrainingEntity()
        {
            ElapsedTime = elapsedTime,
            Time = _startTrainingDateTime,
            Title = TrainingItem.Title,
            TrainingId = TrainingItem.Id,
        });

        foreach (var superSet in Items)
        {
            foreach (var item in superSet)
            {
                if (item.IsSkipped)
                {
                    continue;
                }

                App.Database.SaveLastTrainingExerciseItem(TrainingExerciseMapper.ToLastTrainingExercise(item, id));
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
                App.Database.SaveTrainingExerciseItem(TrainingExerciseMapper.ToEntity(trainingExerciseViewModel, TrainingItem.Id, order));
            }
        }
    }

    private async Task ShareToSocialFeedIfEnabled(TimeSpan elapsedTime)
    {
        if (!Settings.ShareCompletedWorkouts || socialWorkoutsService == null)
        {
            return;
        }

        try
        {
            var request = new ShareSocialWorkoutRequest
            {
                WorkoutName = TrainingItem.Title,
                Date = _startTrainingDateTime,
                Duration = elapsedTime,
            };

            foreach (var superSet in Items)
            {
                foreach (var item in superSet)
                {
                    if (item.IsSkipped)
                    {
                        continue;
                    }

                    var weightAndRepsString = TrainingExerciseMapper.ToWeightAndRepsString(item);

                    if (item.CodeNum > 0)
                    {
                        request.BaseExercises.Add(new SocialWorkoutBaseExerciseDto
                        {
                            CodeNum = item.CodeNum,
                            WeightAndRepsString = weightAndRepsString,
                        });
                    }
                    else
                    {
                        request.CustomExercises.Add(new SocialWorkoutExerciseDto
                        {
                            ExerciseName = item.Name,
                            MusclesString = [.. item.Muscles.Select(muscle => ((MusclesEnum)muscle.Id).ToString())],
                            TagsValue = [.. item.Tags.Select(tag => tag.ToString())],
                            WeightAndRepsString = weightAndRepsString,
                        });
                    }
                }
            }

            await socialWorkoutsService.ShareWorkoutAsync(request);
            LoggingService.TrackEvent("Workout Shared");
        }
        catch (Exception ex)
        {
            LoggingService.TrackError(ex);
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

    private async Task AddExercisesRequest()
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
            var newSuperSet = new SuperSetViewModel<ImplementTrainingExerciseViewModel>() { TrainingId = TrainingItem.Id };
            var superSetExercise = MapBaseSuperSetToImplementMapper.Map(exerciseItem);
            newSuperSet.Add(superSetExercise);

            Items.Add(newSuperSet);
            superSetExercise.TrainingId = TrainingItem.Id;
            superSetExercise.OrderNumber = Items.Count - 1;
            TrainingItem.AddExercise(superSetExercise);
            var id = App.Database.SaveTrainingExerciseItem(TrainingExerciseMapper.ToEntity(superSetExercise));
            superSetExercise.TrainingExerciseId = id;
        }

        OnPropertyChanged(nameof(Items));
    }

    private async void CancelTrainingClicked(object sender, TappedEventArgs e)
    {
        var result = await MessageManager.DisplayAlert(AppResources.CancelTrainingQuestion, TrainingItem.Title, AppResources.YesString, AppResources.NoString);
        if (result)
        {
            enabledTimer = false;
            StopWatchingWindow();
            notificator.Cancel(PushMessagesExtensions.TrainingImplementTimeId);
            ClearNotFinishedTraining();

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
    public ICommand AddExercisesCommand => new AsyncRelayCommand(AddExercisesRequest);

    public string CurrentTime { get; set; }

    public TimeSpan StartTime { get; set; }

    public ObservableCollection<SuperSetViewModel<ImplementTrainingExerciseViewModel>> Items { get; set; }

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