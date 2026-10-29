using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Common.Extensions;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Mappers;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.Views;

namespace TrainingDay.Maui.ViewModels.Pages;

public class HistoryTrainingPageViewModel : BaseViewModel
{
    private const int PageSize = 10;

    private readonly List<HistoryPeriod> daysAndTextLimits;
    private List<LastTrainingEntity> baseItems = [];
    private Dictionary<int, int> exerciseCounts = [];
    private int loadedCount;
    private bool needsReload = true;

    // heading of the history group and max age of a training in days (-1 = no limit)
    private sealed record HistoryPeriod(string Heading, int Days);

    public HistoryTrainingPageViewModel()
    {
        LastTrainings = new ObservableCollection<LastTrainingViewModelList>();
        ItemSelectedCommand = new AsyncRelayCommand<LastTraining>(SelectionChanged);
        StartAgainCommand = new AsyncRelayCommand(StartAgainTraining);
        RemoveLastTrainingCommand = new AsyncRelayCommand(RemoveLastTraining);
        RemainingItemsThresholdReachedCommand = new RelayCommand(LoadNextPage);

        daysAndTextLimits =
        [
            new HistoryPeriod(AppResources.WeekString, 7),
            new HistoryPeriod(AppResources.OneMounthString, 31),
            new HistoryPeriod(AppResources.ThreeMounthString, 91),
            new HistoryPeriod(AppResources.HalfYearString, ConstantKeys.HalfYearDays),
            new HistoryPeriod(AppResources.YearString, 365),
            new HistoryPeriod(AppResources.MoreThanYearString, -1),
        ];
    }

    public ObservableCollection<LastTrainingViewModelList> LastTrainings { get; set; }

    public INavigation Navigation { get; set; }

    public LastTraining SelectedTraining { get; set; }

    public ICommand ItemSelectedCommand { get; }

    public ICommand StartAgainCommand { get; }

    public ICommand RemoveLastTrainingCommand { get; }

    public ICommand RemainingItemsThresholdReachedCommand { get; }

    /// <summary>
    /// Reloads the list on first show and after something changed it (delete, new workout);
    /// otherwise coming back from the details keeps the scroll position and loaded pages.
    /// </summary>
    public void LoadItemsIfNeeded()
    {
        if (!needsReload)
            return;

        needsReload = false;
        LastTrainings.Clear();
        loadedCount = 0;
        baseItems = App.Database.GetLastTrainingItems().OrderByDescending(item => item.Time).ToList();
        exerciseCounts = App.Database.GetLastTrainingExerciseItems()
            .GroupBy(item => item.LastTrainingId)
            .ToDictionary(group => group.Key, group => group.Count());

        LoadNextPage();
    }

    private void LoadNextPage()
    {
        if (loadedCount >= baseItems.Count)
            return;

        var culture = Settings.GetLanguage();
        var now = DateTime.Now;
        foreach (var lastTraining in baseItems.Skip(loadedCount).Take(PageSize))
        {
            try
            {
                var exerciseCount = exerciseCounts.GetValueOrDefault(lastTraining.Id);
                var newItem = new LastTraining()
                {
                    ElapsedTime = lastTraining.ElapsedTime,
                    ImplementDateTime = lastTraining.Time,
                    Title = lastTraining.Title,
                    Id = lastTraining.Id,
                    TrainingId = lastTraining.TrainingId,
                    DateText = $"{lastTraining.Time.ToRelativeDateString(now, culture)} · {lastTraining.Time.ToString("t", culture)}",
                    DurationText = lastTraining.ElapsedTime.ToShortDurationString(),
                    ExerciseCountText = exerciseCount > 0
                        ? exerciseCount.ToPluralString(culture, AppResources.ExerciseCountOne, AppResources.ExerciseCountFew, AppResources.ExerciseCountMany)
                        : string.Empty,
                };

                PutItemToListByDate(newItem, now);
            }
            catch (Exception ex)
            {
                LoggingService.TrackError(ex);
            }
        }

        loadedCount += PageSize;
    }

    private async Task SelectionChanged(LastTraining selected)
    {
        SelectedTraining = selected;
        // Items are filled on every open; start clean so reopening doesn't duplicate exercises
        SelectedTraining.Items.Clear();
        var trainingExerciseItems = App.Database.GetLastTrainingExerciseItems().Where(item => item.LastTrainingId == SelectedTraining.Id);
        foreach (var trainingExercise in trainingExerciseItems)
        {
            SelectedTraining.Items.Add(TrainingExerciseMapper.FromLastTrainingExercise(trainingExercise));
        }

        HistoryTrainingExercisesPage page = new HistoryTrainingExercisesPage() { BindingContext = this };
        await Navigation.PushAsync(page);
    }

    private void PutItemToListByDate(LastTraining newItem, DateTime now)
    {
        var period = daysAndTextLimits.First(limit => limit.Days == -1 || now - newItem.ImplementDateTime < TimeSpan.FromDays(limit.Days));

        // Items arrive newest first, so a period's group is always the last one; reuse it across pages instead of repeating the heading
        var group = LastTrainings.LastOrDefault();
        if (group?.Heading != period.Heading)
        {
            group = new LastTrainingViewModelList { Heading = period.Heading };
            LastTrainings.Add(group);
        }

        group.Add(newItem);
    }

    private async Task StartAgainTraining()
    {
        int trId = SelectedTraining.TrainingId;
        TrainingViewModel training = new TrainingViewModel();
        if (trId == 0)
        {
            training.Title = SelectedTraining.Title;
            foreach (var trainingExerciseViewModel in SelectedTraining.Items)
            {
                training.AddExercise(trainingExerciseViewModel);
            }
        }
        else
        {
            training = TrainingViewModel.LoadWithExercises(trId);
        }

        needsReload = true;
        await Navigation.PushAsync(new TrainingImplementPage() { TrainingItem = training, Title = training.Title });
    }

    private async Task RemoveLastTraining()
    {
        var result = await MessageManager.DisplayAlert(AppResources.DeleteTraining, SelectedTraining.Title, AppResources.YesString, AppResources.CancelString);
        if (result)
        {
            App.Database.DeleteLastTraining(SelectedTraining.Id);
            foreach (var trainingExerciseViewModel in SelectedTraining.Items)
            {
                App.Database.DeleteLastTrainingExercise(trainingExerciseViewModel.TrainingExerciseId);
            }

            needsReload = true;
            await Toast.Make(AppResources.DeletedString).Show();
            await Shell.Current.GoToAsync("..");
        }
    }
}

public class LastTrainingViewModelList : ObservableCollection<LastTraining>
{
    public string Heading { get; set; }
}