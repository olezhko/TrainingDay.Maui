using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Maui.Controls;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class TrainingImplementPage : ContentPage
{
    private readonly DateTime _startTrainingDateTime;
    private bool enabledTimer;
    private IDispatcherTimer _timer;

    public ObservableCollection<SuperSetViewModel> Items { get; set; }

    public TrainingViewModel TrainingItem { get; set; }

    public TrainingImplementPage()
    {
        InitializeComponent();
        Items = new ObservableCollection<SuperSetViewModel>();
        BindingContext = this;
        _startTrainingDateTime = DateTime.Now;
        enabledTimer = true;
        StepProgressBarControl.PropertyChanged += StepProgressBarControl_PropertyChanged;
        Settings.IsTrainingNotFinished = true;

        _timer = Shell.Current.Dispatcher.CreateTimer();
        _timer.Tick += OnTimerTick;
        _timer.Interval = TimeSpan.FromSeconds(1);
    }

    private void StepProgressBarControl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StepProgressBar.StepSelected))
        {
            FinishButton.IsVisible = Items[StepProgressBarControl.StepSelected].First().IsNotFinished;
            SkipButton.IsVisible = Items[StepProgressBarControl.StepSelected].First().IsNotFinished;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (StepProgressBarControl.ItemsSource == null)
        {
            Items = TrainingItem.ExercisesBySuperSet;
            StepProgressBarControl.ItemsSource = Items;
            OnPropertyChanged(nameof(Items));

            DeviceDisplay.KeepScreenOn = Settings.IsDisplayOnImplement;

            int index = 0;
            foreach (var item in Items)
            {
                if (!item.SuperSetItems.First().IsNotFinished)
                {
                    StepProgressBarControl.DeselectElement();
                }

                index++;
                StepProgressBarControl.NextElement(index);
            }

            StepProgressBarControl.NextElement(FirstIndexIsNotFinished());
        }

        ToolTipCancelImplementingTraining.Show();
    }

    protected override bool OnBackButtonPressed()
    {
        return true; // cancel back button
    }

    #region Timer
    public string CurrentTime { get; set; }

    public TimeSpan StartTime { get; set; }

    private void OnTimerTick(object sender, EventArgs e)
    {
        CurrentTime = (DateTime.Now - _startTrainingDateTime + StartTime).ToString(@"hh\:mm\:ss");
        OnPropertyChanged(nameof(CurrentTime));

        UpdateTime();

        // save to restore if not finish
        SaveNotFinishedTraining(TrainingItem.Title, TrainingItem.Id);
        UpdateNotifyTimer();

        if (enabledTimer)
        {
            _timer.Stop();
        }
    }

    private void SaveNotFinishedTraining(string title, int id)
    {
        Settings.IsTrainingNotFinishedTime = CurrentTime;
        var fn = "NotFinished.trday";
        var filename = Path.Combine(FileSystem.CacheDirectory, fn);

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
                var name = string.Join(" - ", Items[StepProgressBarControl.StepSelected].Select(a => a.ExerciseItemName));
                //DependencyService.Get<IMessage>().ShowNotification(PushMessagesManager.TrainingImplementTimeId, name, CurrentTime, true, true, false, null);
            }
        }
    }

    private void UpdateTime()
    {
        try
        {
            var exercises = Items[StepProgressBarControl.StepSelected];
            foreach (var item in exercises)
            {
                if (item.Tags.Contains(TrainingDay.Common.ExerciseTags.ExerciseByTime) && item.IsTimeCalculating && item.IsNotFinished)
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
    private async void FinishButtonClicked(object sender, EventArgs e)
    {
        try
        {
            Items[StepProgressBarControl.StepSelected].ForEach(item => item.IsNotFinished = false);
            Items[StepProgressBarControl.StepSelected].ForEach(item => item.IsSkipped = false);

            if (Items.All(a => a.All(item => !item.IsNotFinished || item.IsSkipped)) && enabledTimer)
            {
                await FinishAndSave();
                Analytics.TrackEvent($"{GetType().Name}: Training Implementing Finished");
            }
            else
            {
                StepProgressBarControl.DeselectElement();
                StepProgressBarControl.NextElement(FirstIndexIsNotFinished());
            }
        }
        catch (Exception ex)
        {
            Crashes.TrackError(ex);
            await Navigation.PopAsync();
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
        //DependencyService.Get<IMessage>().CancelNotification(PushMessagesManager.TrainingImplementTimeId);
        Settings.IsTrainingNotFinished = false;
        SaveLastTraining();
        SaveChangedExercises();

        if (Settings.IsShowAdvicesOnImplementing)
        {
            await MessageManager.DisplayAlert(AppResources.AdviceString, AppResources.AdviceAfterTrainingMessage, AppResources.OkString);
        }

        //DependencyService.Get<IAdInterstitial>().ShowAd(Device.RuntimePlatform == Device.Android
        //    ? "ca-app-pub-8728883017081055/7837401616"
        //    : "ca-app-pub-8728883017081055/1550276858");

        //DependencyService.Get<IMessage>().CancelNotification(PushMessagesManager.TrainingNotificationId);

        await Shell.Current.GoToAsync("..");
        await SiteService.SendFinishedWorkout(Settings.Token);
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
                    ExerciseName = item.ExerciseItemName,
                    MusclesString = MusclesConverter.ConvertFromListToString(item.Muscles.ToList()),
                    Description = item.GetExercise().Description,
                    ExerciseImageUrl = item.ExerciseImageUrl,
                    SuperSetId = item.SuperSetId,
                    TagsValue = TrainingDay.Common.ExerciseTools.ConvertTagListToInt(item.Tags),
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
                var exId = App.Database.SaveExerciseItem(trainingExerciseViewModel.GetExercise());
                order++;
                App.Database.SaveTrainingExerciseItem(new TrainingExerciseComm()
                {
                    ExerciseId = exId,
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

    public ICommand AddExercisesCommand => new Command(AddExercisesRequest);
    private async void AddExercisesRequest()
    {
        var vm = new ExerciseListPageViewModel() { Navigation = Navigation };
        vm.ExercisesSelectFinished += async (sender, args) =>
        {
            await Navigation.PopModalAsync(false);
            AddExercises(args.Select(item => new TrainingExerciseViewModel(item.GetExercise(), new TrainingExerciseComm())));
        };

        vm.ExistedExercises = TrainingItem.Exercises;
        await Navigation.PushModalAsync(new NavigationPage(new ExerciseListPage(vm)));
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
            var id = App.Database.SaveTrainingExerciseItem(exerciseItem.GetTrainingExerciseComm());
            exerciseItem.TrainingExerciseId = id;
        }

        OnPropertyChanged(nameof(Items));
    }

    private async void CancelTrainingClicked(object sender, EventArgs e)
    {
        var result = await MessageManager.DisplayAlert(AppResources.CancelTrainingQuestion, string.Empty, AppResources.YesString, AppResources.NoString);
        if (result)
        {
            enabledTimer = false;
            //DependencyService.Get<IMessage>().CancelNotification(PushMessagesManager.TrainingImplementTimeId);
            Settings.IsTrainingNotFinished = false;

            await Shell.Current.GoToAsync("..");
        }
    }

    private async void SkipButtonClicked(object sender, EventArgs e)
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
}